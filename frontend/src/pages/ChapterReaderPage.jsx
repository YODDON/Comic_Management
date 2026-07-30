import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  getChapterPages,
  getComicChapters,
  getComments,
  postComment,
  purchaseChapter,
} from '../services/catalogService'
import CommentThread from '../components/CommentThread'
import { currentCommentAuthor } from '../utils/commentAuthor'
import { recordReadingHistory } from '../services/readingHistoryService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'
import { hasGuestRoleInToken } from '../services/authService'

export default function ChapterReaderPage() {
  const { locale, tr } = useLanguage()
  const { comicId, chapterSlug } = useParams()
  const navigate = useNavigate()
  const isAuthenticated = Boolean(localStorage.getItem('accessToken'))
  const isGuest = hasGuestRoleInToken()
  const comicSlug = localStorage.getItem(`comicSlug:${comicId}`)
  const [pages, setPages] = useState([])
  const [chapters, setChapters] = useState([])
  const [comments, setComments] = useState([])
  const [comment, setComment] = useState('')
  const [anonymous, setAnonymous] = useState(false)
  const [error, setError] = useState('')
  const [locked, setLocked] = useState(false)
  const [purchasing, setPurchasing] = useState(false)
  const [purchaseNotice, setPurchaseNotice] = useState('')

  useEffect(() => {
    window.scrollTo({ top: 0 })
    const resetTask = window.setTimeout(() => {
      setPages([])
      setLocked(false)
      setError('')
      setPurchaseNotice('')
    }, 0)
    Promise.allSettled([
      getChapterPages(comicId, chapterSlug),
      getComicChapters(comicId),
      getComments(comicId),
    ])
      .then(([pageResult, chapterResult, commentResult]) => {
        if (chapterResult.status === 'rejected') throw chapterResult.reason
        const chapterData = chapterResult.value
        setChapters((chapterData.items || chapterData || [])
          .sort((first, second) => first.chapterNumber - second.chapterNumber))

        if (pageResult.status === 'fulfilled') {
          const pageData = pageResult.value
          setPages(pageData.pages || pageData.items || pageData || [])
        } else if (pageResult.reason?.status === 401 || pageResult.reason?.status === 403) {
          setLocked(true)
        } else {
          setError(pageResult.reason?.message || tr('Không thể tải nội dung chapter.', 'Could not load chapter content.'))
        }

        if (commentResult.status === 'fulfilled') {
          const commentData = commentResult.value
          setComments(commentData.items || commentData || [])
        }
      })
      .catch((requestError) => setError(requestError.message))
    return () => window.clearTimeout(resetTask)
  }, [comicId, chapterSlug, tr])

  const index = chapters.findIndex((chapter) => chapter.slug === chapterSlug)
  const current = chapters[index]
  const previous = index > 0 ? chapters[index - 1] : null
  const next = index >= 0 && index < chapters.length - 1 ? chapters[index + 1] : null
  const prefix = current ? `[chapter:${current.id}]` : ''
  const chapterComments = useMemo(
    () => comments.filter((item) => item.content?.startsWith(prefix)),
    [comments, prefix],
  )
  const translationTexts = useMemo(() => chapters.map((chapter) => ({
    key: `chapter.${chapter.id}`,
    value: chapter.title,
  })), [chapters])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)
  const go = (nextSlug) => navigate(`/read/${comicId}/${nextSlug}`)
  const chapterTitle = (chapter) => translations[`chapter.${chapter.id}`] || chapter.title

  async function unlockChapter() {
    if (isGuest) {
      setError(tr(
        'Tài khoản Guest không thể mở khóa chapter trả phí.',
        'Guest accounts cannot unlock paid chapters.',
      ))
      return
    }
    if (!isAuthenticated) {
      navigate('/login', { state: { from: `/read/${comicId}/${chapterSlug}` } })
      return
    }
    if (!current?.id || purchasing) return

    setPurchasing(true)
    setError('')
    setPurchaseNotice('')
    try {
      const result = await purchaseChapter(current.id)
      const refreshedPages = await getChapterPages(comicId, chapterSlug)
      setPages(refreshedPages.pages || refreshedPages.items || refreshedPages || [])
      setLocked(false)
      const balanceText = result.remainingBalance == null
        ? ''
        : ` ${tr('Số dư còn lại', 'Remaining balance')}: ${Number(result.remainingBalance).toLocaleString(locale)} ${tr('Dâu', 'Berries')}.`
      setPurchaseNotice(`${result.alreadyPurchased ? tr('Chapter đã được mở khóa.', 'Chapter is already unlocked.') : tr('Mở khóa chapter thành công.', 'Chapter unlocked successfully.')}${balanceText}`)
    } catch (requestError) {
      const balanceText = requestError.data?.remainingBalance == null
        ? ''
        : ` ${tr('Số dư hiện tại', 'Current balance')}: ${Number(requestError.data.remainingBalance).toLocaleString(locale)} ${tr('Dâu', 'Berries')}.`
      setError(`${requestError.message}${balanceText}`)
    } finally {
      setPurchasing(false)
    }
  }

  async function refreshComments() {
    const data = await getComments(comicId)
    setComments(data.items || data || [])
  }

  useEffect(() => {
    if (!current?.id || !isAuthenticated || isGuest) return
    recordReadingHistory(comicId, current.id, {
      comicSlug,
      chapterSlug: current.slug,
      chapterNumber: current.chapterNumber,
      chapterTitle: current.title,
    }).catch(() => {})
  }, [
    comicId,
    comicSlug,
    current?.chapterNumber,
    current?.id,
    current?.slug,
    current?.title,
    isAuthenticated,
    isGuest,
  ])

  async function submit(event) {
    event.preventDefault()
    if (isGuest) {
      setError(tr('Tài khoản Guest chỉ được xem bình luận.', 'Guest accounts can only view comments.'))
      return
    }
    if (!isAuthenticated) {
      navigate('/login')
      return
    }

    const content = `${prefix}[author:${currentCommentAuthor(anonymous)}]${comment}`
    const temporaryId = `temporary-${Date.now()}`
    const temporaryComment = {
      id: temporaryId,
      comicId,
      content,
      createdAt: new Date().toISOString(),
      replies: [],
    }
    setComments((currentComments) => [temporaryComment, ...currentComments])
    setComment('')
    setAnonymous(false)
    setError('')

    try {
      const created = await postComment(comicId, content)
      if (created?.id) {
        setComments((currentComments) =>
          currentComments.map((item) => item.id === temporaryId ? created : item))
      }
    } catch (requestError) {
      setComments((currentComments) =>
        currentComments.filter((item) => item.id !== temporaryId))
      setError(requestError.message)
    }
  }

  async function reply(content, parentId, isAnonymous) {
    if (isGuest) {
      setError(tr('Tài khoản Guest chỉ được xem bình luận.', 'Guest accounts can only view comments.'))
      return
    }
    if (!isAuthenticated) {
      navigate('/login')
      return
    }
    await postComment(
      comicId,
      `${prefix}[author:${currentCommentAuthor(isAnonymous)}]${content}`,
      parentId,
    )
    await refreshComments()
  }

  return (
    <div className="reader-page">
      <header>
        <Link className="reader-back-link" to={comicSlug ? `/comics/${comicSlug}` : '/'}>
          <svg viewBox="0 0 16 16" aria-hidden="true"><path d="m10 3-5 5 5 5" /></svg>
          <span>{tr('Quay lại truyện', 'Back to comic')}</span>
        </Link>
        <b>{current ? `${tr('Chương', 'Chapter')} ${current.chapterNumber}: ${chapterTitle(current)}` : chapterSlug}</b>
        <select value={chapterSlug} onChange={(event) => go(event.target.value)}>
          {chapters.map((chapter) => (
            <option key={chapter.id} value={chapter.slug}>
              {tr('Chương', 'Chapter')} {chapter.chapterNumber}: {chapterTitle(chapter)}
            </option>
          ))}
        </select>
      </header>

      <nav className="reader-navigation">
        <button disabled={!previous} onClick={() => previous && go(previous.slug)}>
          ← {tr('Chap trước', 'Previous chapter')}
        </button>
        <button disabled={!next} onClick={() => next && go(next.slug)}>
          {tr('Chap sau', 'Next chapter')} →
        </button>
      </nav>
      {error && <div className="reader-error">{error}</div>}
      {translationError && <div className="reader-error">{translationError}</div>}
      {translating && <div className="reader-translation-note">{tr('Đang dịch tên chapter; ảnh đọc truyện vẫn giữ nguyên.', 'Translating chapter titles; comic images remain unchanged.')}</div>}
      {purchaseNotice && <div className="reader-purchase-success">{purchaseNotice}</div>}

      {locked && current && (
        <section className="chapter-unlock-card">
          <div className="chapter-unlock-icon">🔒</div>
          <h2>{tr('Chapter trả phí', 'Paid chapter')}</h2>
          <p>
            {tr('Mở khóa chapter này với', 'Unlock this chapter for')}{' '}
            <strong>{Number(current.unitPrice || 0).toLocaleString(locale)} {tr('Dâu', 'Berries')}</strong>.
          </p>
          <button type="button" disabled={purchasing} onClick={unlockChapter}>
            {purchasing
              ? tr('Đang mở khóa...', 'Unlocking...')
              : isGuest
                ? tr('Guest không thể mở khóa', 'Guests cannot unlock')
                : isAuthenticated
                ? `${tr('Mở khóa', 'Unlock')} ${Number(current.unitPrice || 0).toLocaleString(locale)} ${tr('Dâu', 'Berries')}`
                : tr('Đăng nhập để mở khóa', 'Sign in to unlock')}
          </button>
        </section>
      )}

      <main>
        {pages.length === 0 && !error && !locked && (
          <div className="reader-loading">{tr('Đang tải hình ảnh...', 'Loading images...')}</div>
        )}
        {pages.map((page, pageIndex) => (
          <img
            key={page.id || pageIndex}
            src={page.imageUrl || page.url}
            alt={`${tr('Trang', 'Page')} ${pageIndex + 1}`}
          />
        ))}
      </main>

      <nav className="reader-navigation reader-navigation-bottom">
        <button disabled={!previous} onClick={() => previous && go(previous.slug)}>
          ← {tr('Chap trước', 'Previous chapter')}
        </button>
        <button disabled={!next} onClick={() => next && go(next.slug)}>
          {tr('Chap sau', 'Next chapter')} →
        </button>
      </nav>

      <section className="reader-comments">
        <h2>{tr('Bình luận chương', 'Chapter comments')} ({chapterComments.length})</h2>
        {isAuthenticated && !isGuest ? (
          <form onSubmit={submit}>
            <textarea
              required
              value={comment}
              onChange={(event) => setComment(event.target.value)}
              placeholder={tr('Bình luận về chapter này...', 'Comment on this chapter...')}
            />
            <div className="comment-form-actions">
              <button>{tr('Gửi bình luận', 'Post comment')}</button>
              <label>
                <input
                  type="checkbox"
                  checked={anonymous}
                  onChange={(event) => setAnonymous(event.target.checked)}
                />
                {tr('Bình luận ẩn danh', 'Comment anonymously')}
              </label>
            </div>
          </form>
        ) : (
          isGuest
            ? <div className="comment-login-required dark">{tr('Tài khoản Guest chỉ được xem bình luận.', 'Guest accounts can only view comments.')}</div>
            : <div className="comment-login-required dark">{tr('Bạn cần', 'You need to')} <Link to="/login">{tr('đăng nhập', 'sign in')}</Link> {tr('để bình luận hoặc trả lời.', 'to comment or reply.')}</div>
        )}
        {chapterComments.map((item) => (
          <CommentThread
            key={item.id}
            comment={item}
            prefix={prefix}
            onReply={reply}
            canReply={isAuthenticated && !isGuest}
          />
        ))}
      </section>
    </div>
  )
}
