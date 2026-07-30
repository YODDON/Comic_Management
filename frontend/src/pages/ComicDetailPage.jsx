import { useEffect, useMemo, useState } from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import CommentThread from '../components/CommentThread'
import {
  getComic,
  getComicChapters,
  getComments,
  postComment,
} from '../services/catalogService'
import { hasInLibrary, toggleLibrary } from '../services/libraryService'
import { currentCommentAuthor } from '../utils/commentAuthor'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'
import { hasGuestRoleInToken } from '../services/authService'

export default function ComicDetailPage() {
  const { locale, tr } = useLanguage()
  const { slug } = useParams()
  const navigate = useNavigate()
  const isAuthenticated = Boolean(localStorage.getItem('accessToken'))
  const isGuest = hasGuestRoleInToken()
  const [comic, setComic] = useState(null)
  const [chapters, setChapters] = useState([])
  const [comments, setComments] = useState([])
  const [comment, setComment] = useState('')
  const [anonymous, setAnonymous] = useState(false)
  const [error, setError] = useState('')
  const [missing, setMissing] = useState(false)
  const [tab, setTab] = useState('chapters')
  const [favorite, setFavorite] = useState(false)
  const [following, setFollowing] = useState(false)

  async function refreshComments(comicId) {
    const data = await getComments(comicId)
    setComments(data.items || data || [])
  }

  useEffect(() => {
    getComic(slug)
      .then(async (data) => {
        localStorage.setItem(`comicSlug:${data.id}`, data.slug)
        setComic(data)
        setFavorite(isAuthenticated && hasInLibrary('favorite', data.id))
        setFollowing(isAuthenticated && hasInLibrary('following', data.id))
        const [chapterData, commentData] = await Promise.all([
          getComicChapters(data.id),
          getComments(data.id).catch(() => []),
        ])
        setChapters(chapterData.items || chapterData || [])
        setComments(commentData.items || commentData || [])
      })
      .catch(() => setMissing(true))
  }, [isAuthenticated, slug])

  const comicComments = useMemo(
    () => comments.filter((item) => !item.content?.startsWith('[chapter:')),
    [comments],
  )
  const translationTexts = useMemo(() => comic ? [
    { key: 'comic.title', value: comic.title },
    { key: 'comic.description', value: comic.description },
    { key: 'comic.author', value: comic.authorName },
    ...(comic.categories || []).map((category) => ({
      key: `category.${category.id}`,
      value: category.name,
    })),
    ...chapters.map((chapter) => ({
      key: `chapter.${chapter.id}`,
      value: chapter.title,
    })),
  ] : [], [chapters, comic])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  if (missing) return <Navigate to="/" replace />
  if (!comic) {
    return (
      <div className="comic-detail-page">
        <SiteHeader />
        <div className="detail-loading">{tr('Đang tải truyện...', 'Loading comic...')}</div>
      </div>
    )
  }

  const ordered = [...chapters].sort((a, b) => a.chapterNumber - b.chapterNumber)
  const first = ordered[0]
  const latest = ordered[ordered.length - 1]
  const displayTitle = translations['comic.title'] || comic.title
  const displayDescription = translations['comic.description'] || comic.description
  const displayAuthor = translations['comic.author'] || comic.authorName || 'Comico'
  const chapterTitle = (chapter) => translations[`chapter.${chapter.id}`] || chapter.title

  function requireLogin() {
    setError(tr('Bạn cần đăng nhập để sử dụng chức năng này.', 'You need to sign in to use this feature.'))
    navigate('/login', { state: { from: `/comics/${slug}` } })
  }

  function denyGuest() {
    setError(tr(
      'Tài khoản Guest chỉ được đọc truyện, tìm truyện và xem bảng xếp hạng.',
      'Guest accounts can only read comics, discover comics and view rankings.',
    ))
  }

  function toggle(kind, setter) {
    if (isGuest) {
      denyGuest()
      return
    }
    if (!isAuthenticated) {
      requireLogin()
      return
    }
    setter(toggleLibrary(kind, comic, chapters.length))
    setError('')
  }

  async function submitComment(event) {
    event.preventDefault()
    if (isGuest) {
      denyGuest()
      return
    }
    if (!isAuthenticated) {
      requireLogin()
      return
    }

    const content = `[author:${currentCommentAuthor(anonymous)}]${comment}`
    const temporaryId = `temporary-${Date.now()}`
    const temporaryComment = {
      id: temporaryId,
      comicId: comic.id,
      content,
      createdAt: new Date().toISOString(),
      replies: [],
    }
    setComments((currentComments) => [temporaryComment, ...currentComments])
    setComment('')
    setAnonymous(false)
    setError('')

    try {
      const created = await postComment(comic.id, content)
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
      denyGuest()
      return
    }
    if (!isAuthenticated) {
      requireLogin()
      return
    }
    await postComment(
      comic.id,
      `[author:${currentCommentAuthor(isAnonymous)}]${content}`,
      parentId,
    )
    await refreshComments(comic.id)
  }

  return (
    <div className="comic-detail-page">
      <SiteHeader />
      <main>
        <div className="detail-breadcrumb">
          <Link to="/">{tr('Trang chủ', 'Home')}</Link> / {displayTitle}
        </div>
        {error && <div className="detail-error">{error}</div>}
        <section className="comic-hero">
          <div
            className="detail-cover"
            style={{ backgroundImage: `url("${comic.coverUrl}")` }}
          />
          <div className="detail-info">
            <h1>{displayTitle}</h1>
            <p className="detail-author">✎ {displayAuthor}</p>
            <div className="detail-stats">
              <span><b>{comic.viewCount || 0}</b>{tr('Lượt xem', 'Views')}</span>
              <span><b>{comic.chapterCount || chapters.length}</b>{tr('Chương', 'Chapters')}</span>
              <span><b>{comic.isOutstanding ? tr('Có', 'Yes') : tr('Không', 'No')}</b>{tr('Nổi bật', 'Featured')}</span>
            </div>
            <div className="detail-categories">
              {(comic.categories || []).map((category) => (
                <Link key={category.id} to={`/search?category=${category.id}`}>
                  {translations[`category.${category.id}`] || category.name}
                </Link>
              ))}
            </div>
            {translationError && <div className="translation-error">{translationError}</div>}
            {translating && <div className="translation-progress">{tr('Đang dịch phần chữ... Ảnh chapter vẫn giữ nguyên.', 'Translating text... Chapter images remain unchanged.')}</div>}
            <p className="detail-description">{displayDescription || tr('Chưa có mô tả.', 'No description available.')}</p>
            <div className="detail-actions detail-actions-four">
              {first
                ? <Link className="primary" to={`/read/${comic.id}/${first.slug}`}><i>▣</i> {tr('Đọc từ đầu', 'Read from start')}</Link>
                : <button disabled>{tr('Chưa có chương', 'No chapters yet')}</button>}
              {latest && (
                <Link to={`/read/${comic.id}/${latest.slug}`}>
                  <i>▶</i> {tr('Chapter mới nhất', 'Latest chapter')}
                </Link>
              )}
              <button
                className={favorite ? 'selected' : ''}
                onClick={() => toggle('favorite', setFavorite)}
              >
                <i>♥</i>{favorite ? tr('Đã yêu thích', 'Favorited') : tr('Yêu thích', 'Favorite')}
              </button>
              <button
                className={following ? 'selected' : ''}
                onClick={() => toggle('following', setFollowing)}
              >
                <i>♟</i>{following ? tr('Đang theo dõi', 'Following') : tr('Theo dõi', 'Follow')}
              </button>
            </div>
            {(!isAuthenticated || isGuest) && (
              <p className="auth-required-note">
                {isGuest
                  ? tr('Tài khoản Guest chỉ có quyền đọc truyện.', 'Guest accounts have read-only access.')
                  : tr('Đăng nhập để yêu thích, theo dõi và bình luận truyện.', 'Sign in to favorite, follow and comment on comics.')}
              </p>
            )}
          </div>
        </section>

        <div className="detail-tabs">
          <button
            className={tab === 'chapters' ? 'active' : ''}
            onClick={() => setTab('chapters')}
          >
            ☷ {tr('Danh sách chương', 'Chapter list')} ({chapters.length})
          </button>
          <button
            className={tab === 'comments' ? 'active' : ''}
            onClick={() => setTab('comments')}
          >
            ☁ {tr('Bình luận', 'Comments')} ({comicComments.length})
          </button>
        </div>

        {tab === 'chapters' && (
          <section className="detail-panel tab-panel">
            <div className="chapter-public-list">
              {ordered.map((chapter) => (
                <Link key={chapter.id} to={`/read/${comic.id}/${chapter.slug}`}>
                  <b>{tr('Chương', 'Chapter')} {chapter.chapterNumber}: {chapterTitle(chapter)}</b>
                  <span className="chapter-row-meta">
                    <em className={Number(chapter.unitPrice) === 0 ? 'free' : chapter.isPurchased ? 'unlocked' : ''}>
                      {Number(chapter.unitPrice) === 0
                        ? tr('Miễn phí', 'Free')
                        : chapter.isPurchased
                          ? tr('Đã mở khóa', 'Unlocked')
                          : `${Number(chapter.unitPrice).toLocaleString(locale)} ${tr('Dâu', 'Berries')}`}
                    </em>
                    {new Date(chapter.createdAt).toLocaleDateString(locale)}
                  </span>
                </Link>
              ))}
            </div>
          </section>
        )}

        {tab === 'comments' && (
          <section className="detail-panel comments-panel tab-panel">
            {isAuthenticated && !isGuest ? (
              <form onSubmit={submitComment}>
                <textarea
                  required
                  value={comment}
                  onChange={(event) => setComment(event.target.value)}
                  placeholder={tr('Viết bình luận về truyện...', 'Write a comment about this comic...')}
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
                ? <div className="comment-login-required">{tr('Tài khoản Guest chỉ được xem bình luận.', 'Guest accounts can only view comments.')}</div>
                : <div className="comment-login-required">{tr('Bạn cần', 'You need to')} <Link to="/login">{tr('đăng nhập', 'sign in')}</Link> {tr('để bình luận hoặc trả lời.', 'to comment or reply.')}</div>
            )}
            <div>
              {comicComments.map((item) => (
                <CommentThread
                  key={item.id}
                  comment={item}
                  onReply={reply}
                  canReply={isAuthenticated && !isGuest}
                />
              ))}
            </div>
          </section>
        )}
      </main>
    </div>
  )
}
