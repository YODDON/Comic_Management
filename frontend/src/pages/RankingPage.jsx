import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import { getRankingComics } from '../services/catalogService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

export default function RankingPage() {
  const { tr } = useLanguage()
  const [items, setItems] = useState([])
  const translationTexts = useMemo(() => items.flatMap((comic) => [
    { key: `comic.${comic.id}.title`, value: comic.title },
    { key: `comic.${comic.id}.author`, value: comic.authorName },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)
  useEffect(() => { getRankingComics().then((data) => setItems((data.items || data || []).filter((x) => !['Dropped', 'Rejected', 'Từ chối'].includes(x.status)).sort((a, b) => b.viewCount - a.viewCount).slice(0, 10))).catch(() => {}) }, [])
  return <div className="ranking-page"><SiteHeader />{translationError && <div className="global-translation-alert">{translationError}</div>}<main><div className="catalog-heading"><p>{tr('BẢNG XẾP HẠNG', 'RANKING')}</p><h1>{tr('Top 10 truyện nhiều lượt xem', 'Top 10 most viewed comics')}</h1></div><div className="ranking-list">{items.map((comic, index) => <Link to={`/comics/${comic.slug}`} key={comic.id}><strong className={`rank-number rank-${index + 1}`}>#{index + 1}</strong><div className="rank-cover" style={{ backgroundImage: `url("${comic.coverUrl}")` }} /><div><h2>{translations[`comic.${comic.id}.title`] || comic.title}</h2><p>{translations[`comic.${comic.id}.author`] || comic.authorName || 'Comico'}</p></div><b>{comic.viewCount || 0} {tr('lượt xem', 'views')}</b></Link>)}</div>{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}</main></div>
}
