import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import ComicCard from '../components/ComicCard'
import Pagination from '../components/Pagination'
import { getLibrary } from '../services/libraryService'
import { getComic, getComicById, getComicChapters } from '../services/catalogService'
import { getLocalReadingHistory, getReadingHistory, getReadingMetadata } from '../services/readingHistoryService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

const PAGE_SIZE = 5

export default function LibraryPage() {
  const { locale, tr } = useLanguage()
  const [tab, setTab] = useState('favorite')
  const [page, setPage] = useState(1)
  const [items, setItems] = useState(() => getLibrary('favorite'))
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(false)

  const activeItems = tab === 'history' ? history : items
  const totalPages = Math.max(1, Math.ceil(activeItems.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const pageStart = (safePage - 1) * PAGE_SIZE
  const pagedItems = useMemo(
    () => tab === 'history' ? [] : items.slice(pageStart, pageStart + PAGE_SIZE),
    [items, pageStart, tab],
  )
  const pagedHistory = useMemo(
    () => tab === 'history' ? history.slice(pageStart, pageStart + PAGE_SIZE) : [],
    [history, pageStart, tab],
  )

  const translationTexts = useMemo(() => [
    ...pagedItems.flatMap((comic) => [
      { key: `comic.${comic.id}.title`, value: comic.title },
      { key: `comic.${comic.id}.author`, value: comic.authorName },
    ]),
    ...pagedHistory.flatMap((item) => [
      { key: `comic.${item.comic.id}.title`, value: item.comic.title },
      { key: `chapter.${item.chapter?.id}.title`, value: item.chapter?.title },
    ]),
  ], [pagedHistory, pagedItems])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  const chooseTab = (nextTab) => {
    setPage(1)
    if (nextTab === 'history') setLoading(true)
    setTab(nextTab)
  }

  useEffect(() => {
    if (tab === 'history') {
      let active = true
      getReadingHistory()
        .catch(() => ({ items: [] }))
        .then(async (historyData) => {
          const serverRecords = historyData.items || historyData || []
          const localRecords = getLocalReadingHistory()
          const records = [
            ...serverRecords,
            ...localRecords.filter((local) => !serverRecords.some((server) => String(server.comicId) === String(local.comicId))),
          ]
          const enriched = await Promise.all(records.map(async (record) => {
            const metadata = getReadingMetadata(record.comicId)
            const comic = await getComicById(record.comicId)
              .catch(() => metadata?.comicSlug ? getComic(metadata.comicSlug).catch(() => null) : null)
            if (!comic) return null
            const chapterData = await getComicChapters(record.comicId).catch(() => [])
            const chapters = chapterData.items || chapterData || []
            const chapter = chapters.find((item) => String(item.id) === String(record.chapterId))
              || (metadata?.chapterSlug
                ? {
                    id: record.chapterId,
                    slug: metadata.chapterSlug,
                    chapterNumber: metadata.chapterNumber,
                    title: metadata.chapterTitle,
                  }
                : null)
            return { ...record, comic, chapter }
          }))
          if (active) setHistory(enriched.filter(Boolean))
        })
        .catch(() => { if (active) setHistory([]) })
        .finally(() => { if (active) setLoading(false) })
      return () => { active = false }
    }

    const load = () => setItems(getLibrary(tab))
    load()
    window.addEventListener('library-updated', load)
    return () => window.removeEventListener('library-updated', load)
  }, [tab])

  function changePage(nextPage) {
    setPage(nextPage)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <div className="library-page">
      <SiteHeader />
      {translationError && <div className="global-translation-alert">{translationError}</div>}
      <main>
        <p className="section-eyebrow">{tr('THƯ VIỆN CỦA TÔI', 'MY LIBRARY')}</p>
        <h1>{tr('Truyện của bạn', 'Your comics')}</h1>
        <div className="library-tabs">
          <button className={tab === 'favorite' ? 'active' : ''} onClick={() => chooseTab('favorite')}>♥ {tr('Truyện yêu thích', 'Favorites')}</button>
          <button className={tab === 'following' ? 'active' : ''} onClick={() => chooseTab('following')}>♟ {tr('Truyện theo dõi', 'Following')}</button>
          <button className={tab === 'history' ? 'active' : ''} onClick={() => chooseTab('history')}>◷ {tr('Lịch sử đọc', 'Reading history')}</button>
        </div>

        {tab === 'history' ? (
          loading ? (
            <div className="library-empty">{tr('Đang tải lịch sử đọc...', 'Loading reading history...')}</div>
          ) : pagedHistory.length > 0 ? (
            <div className="reading-history-list">
              {pagedHistory.map((item) => {
                const target = item.chapter ? `/read/${item.comic.id}/${item.chapter.slug}` : `/comics/${item.comic.slug}`
                const title = translations[`comic.${item.comic.id}.title`] || item.comic.title
                const chapterTitle = translations[`chapter.${item.chapter?.id}.title`] || item.chapter?.title
                return (
                  <article key={item.id}>
                    <Link className="history-cover" to={target} style={{ backgroundImage: `url("${item.comic.coverUrl}")` }} />
                    <div>
                      <Link to={target}><h3>{title}</h3></Link>
                      <p>{item.chapter ? tr(`Đã đọc đến Chương ${item.chapter.chapterNumber}: ${chapterTitle}`, `Read through Chapter ${item.chapter.chapterNumber}: ${chapterTitle}`) : tr('Chapter đã đọc', 'Last read chapter')}</p>
                      <time>{tr('Đọc lúc', 'Read at')} {new Date(item.readAt).toLocaleString(locale)}</time>
                      <Link className="continue-reading" to={target}>{tr('Đọc tiếp →', 'Continue reading →')}</Link>
                    </div>
                  </article>
                )
              })}
            </div>
          ) : (
            <div className="library-empty">{tr('Bạn chưa đọc truyện nào.', 'You have not read any comics yet.')}</div>
          )
        ) : pagedItems.length > 0 ? (
          <div className="library-grid">
            {pagedItems.map((comic) => (
              <ComicCard
                key={comic.id}
                comic={comic}
                title={translations[`comic.${comic.id}.title`]}
                author={translations[`comic.${comic.id}.author`]}
                featuredLabel={tr('Nổi bật', 'Featured')}
              />
            ))}
          </div>
        ) : (
          <div className="library-empty">
            {tab === 'favorite'
              ? tr('Bạn chưa yêu thích truyện nào.', 'You have no favorite comics yet.')
              : tr('Bạn chưa theo dõi truyện nào.', 'You are not following any comics yet.')}
          </div>
        )}

        {!loading && (
          <Pagination
            page={safePage}
            totalItems={activeItems.length}
            pageSize={PAGE_SIZE}
            onChange={changePage}
          />
        )}
        {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
      </main>
    </div>
  )
}
