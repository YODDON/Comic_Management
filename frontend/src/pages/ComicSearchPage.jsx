import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import ComicCard from '../components/ComicCard'
import Pagination from '../components/Pagination'
import { getCategories, searchComics } from '../services/catalogService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

const PAGE_SIZE = 16
const allowed = (comic) => !['Dropped', 'Rejected', 'Từ chối'].includes(comic.status)

export default function ComicSearchPage() {
  const { tr } = useLanguage()
  const [params, setParams] = useSearchParams()
  const [categories, setCategories] = useState([])
  const [items, setItems] = useState([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState(params.get('q') || '')
  const selected = params.getAll('category')
  const pageValue = Number.parseInt(params.get('page') || '1', 10)
  const page = Number.isFinite(pageValue) && pageValue > 0 ? pageValue : 1

  const translationTexts = useMemo(() => [
    ...categories.map((category) => ({ key: `category.${category.id}`, value: category.name })),
    ...items.flatMap((comic) => [
      { key: `comic.${comic.id}.title`, value: comic.title },
      { key: `comic.${comic.id}.author`, value: comic.authorName },
    ]),
  ], [categories, items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  useEffect(() => {
    getCategories().then((result) => setCategories(result.items || result || [])).catch(() => {})
  }, [])

  useEffect(() => {
    let active = true
    const timer = window.setTimeout(() => {
      setLoading(true)
      searchComics({
        search: params.get('q') || '',
        categoryIds: params.getAll('category'),
        page,
        pageSize: PAGE_SIZE,
      })
        .then((result) => {
          if (!active) return
          setItems((result.items || result || []).filter(allowed))
          setTotal(result.totalCount ?? result.total ?? 0)
        })
        .catch(() => {
          if (!active) return
          setItems([])
          setTotal(0)
        })
        .finally(() => { if (active) setLoading(false) })
    }, 0)
    return () => {
      active = false
      window.clearTimeout(timer)
    }
  }, [page, params])

  function applyCategory(id) {
    const next = selected.includes(id)
      ? selected.filter((categoryId) => categoryId !== id)
      : [...selected, id]
    const query = new URLSearchParams()
    if (params.get('q')) query.set('q', params.get('q'))
    next.forEach((categoryId) => query.append('category', categoryId))
    setParams(query)
  }

  function submit(event) {
    event.preventDefault()
    const query = new URLSearchParams()
    if (search.trim()) query.set('q', search.trim())
    selected.forEach((categoryId) => query.append('category', categoryId))
    setParams(query)
  }

  function changePage(nextPage) {
    const query = new URLSearchParams(params)
    if (nextPage > 1) query.set('page', String(nextPage))
    else query.delete('page')
    setParams(query)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <div className="catalog-page">
      <SiteHeader />
      {translationError && <div className="global-translation-alert">{translationError}</div>}
      <main>
        <div className="catalog-heading">
          <p>{tr('KHÁM PHÁ', 'DISCOVER')}</p>
          <h1>{tr('Tìm truyện', 'Find comics')}</h1>
        </div>
        <form className="catalog-filters multi-category-filter" onSubmit={submit}>
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder={tr('Nhập tên truyện...', 'Enter a comic title...')} />
          <button>{tr('Tìm kiếm', 'Search')}</button>
          <fieldset>
            <legend>{tr('Chọn một hoặc nhiều thể loại', 'Choose one or more genres')}</legend>
            {categories.map((category) => (
              <label key={category.id}>
                <input type="checkbox" checked={selected.includes(category.id)} onChange={() => applyCategory(category.id)} />
                {translations[`category.${category.id}`] || category.name}
              </label>
            ))}
          </fieldset>
        </form>

        {loading ? (
          <div className="catalog-empty">{tr('Đang tải danh sách truyện...', 'Loading comics...')}</div>
        ) : (
          <>
            <div className="public-comic-grid">
              {items.map((comic) => (
                <ComicCard
                  key={comic.id}
                  comic={comic}
                  title={translations[`comic.${comic.id}.title`]}
                  author={translations[`comic.${comic.id}.author`]}
                  featuredLabel={tr('Nổi bật', 'Featured')}
                />
              ))}
            </div>
            <Pagination page={page} totalItems={total} pageSize={PAGE_SIZE} onChange={changePage} />
            {items.length === 0 && <div className="catalog-empty">{tr('Không tìm thấy truyện phù hợp.', 'No matching comics found.')}</div>}
          </>
        )}
        {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
      </main>
    </div>
  )
}
