import { useEffect, useMemo, useState } from 'react'
import { Navigate, useParams } from 'react-router-dom'
import ComicCard from '../components/ComicCard'
import Pagination from '../components/Pagination'
import SiteHeader from '../components/SiteHeader'
import { getOutstandingComicsPaged, getPublicComics } from '../services/catalogService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

const PAGE_SIZE = 12
const configurations = {
  featured: {
    title: 'Truyện nổi bật',
    description: 'Tất cả truyện được Admin đánh dấu nổi bật.',
    titleEn: 'Featured comics',
    descriptionEn: 'All comics marked as featured by the administrator.',
  },
  recommended: {
    title: 'Đề xuất truyện hay',
    description: 'Danh sách truyện mới được cập nhật trên Comico.',
    titleEn: 'Recommended comics',
    descriptionEn: 'Newly updated comics on Comico.',
  },
}

const isVisible = (comic) => !['Dropped', 'Rejected'].includes(comic.status)

export default function ComicCollectionPage() {
  const { tr } = useLanguage()
  const { type } = useParams()
  const config = configurations[type]
  const [items, setItems] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!config) return
    const timer = window.setTimeout(() => {
      setLoading(true)
      setError('')

      const task = type === 'featured'
        ? getOutstandingComicsPaged({ page, pageSize: PAGE_SIZE }).then((data) => {
            setItems((data.items || []).filter(isVisible))
            setTotal(data.totalCount || 0)
          })
        : getPublicComics({ page, pageSize: PAGE_SIZE }).then((data) => {
            setItems((data.items || []).filter(isVisible))
            setTotal(data.totalCount || 0)
          })

      task
        .catch((requestError) => setError(requestError.message))
        .finally(() => setLoading(false))
    }, 0)
    return () => window.clearTimeout(timer)
  }, [config, page, type])

  const displayedItems = useMemo(() => items, [items])
  const translationTexts = useMemo(() => items.flatMap((comic) => [
    { key: `comic.${comic.id}.title`, value: comic.title },
    { key: `comic.${comic.id}.author`, value: comic.authorName },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  if (!config) return <Navigate to="/" replace />

  return (
    <div className="catalog-page">
      <SiteHeader />
      <main>
        <div className="catalog-heading">
          <p>COMICO</p>
          <h1>{tr(config.title, config.titleEn)}</h1>
        </div>
        <p className="collection-description">{tr(config.description, config.descriptionEn)}</p>

        {error && <div className="detail-error">{error}</div>}
        {translationError && <div className="detail-error">{translationError}</div>}
        {loading ? (
          <div className="catalog-empty">{tr('Đang tải danh sách truyện...', 'Loading comics...')}</div>
        ) : displayedItems.length ? (
          <>
            <div className="public-comic-grid">
              {displayedItems.map((comic) => <ComicCard key={comic.id} comic={comic} title={translations[`comic.${comic.id}.title`]} author={translations[`comic.${comic.id}.author`]} featuredLabel={tr('Nổi bật', 'Featured')} />)}
            </div>
            <Pagination
              page={page}
              totalItems={total}
              pageSize={PAGE_SIZE}
              onChange={(value) => {
                setPage(value)
                window.scrollTo({ top: 0, behavior: 'smooth' })
              }}
            />
          </>
        ) : (
          <div className="catalog-empty">{tr('Chưa có truyện trong danh sách này.', 'There are no comics in this list yet.')}</div>
        )}
        {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
      </main>
    </div>
  )
}
