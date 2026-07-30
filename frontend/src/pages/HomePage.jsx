import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import ComicCard from '../components/ComicCard'
import { getBanners } from '../services/homeService'
import { getNewComics, getOutstandingComics } from '../services/catalogService'
import { trackLobbyMinute } from '../services/missionService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'
import { hasGuestRoleInToken } from '../services/authService'

const fallbackBanner = {
  title: 'Truyện tranh nổi bật',
  imageUrl: '/images/hero-banner.png',
  linkUrl: '#featured-comics',
}

const visibleComics = (items) =>
  (items || []).filter((comic) => !['Dropped', 'Rejected'].includes(comic.status))

function ComicSection({ id, title, items, target, translations, tr }) {
  return (
    <section className="comic-section home-catalog-section" id={id}>
      <div className="section-heading">
        <div><h2>{title}</h2></div>
        <Link to={target}>{tr('Xem tất cả >', 'View all >')}</Link>
      </div>
      <div className="public-comic-grid">
        {items.map((comic) => <ComicCard
          key={comic.id}
          comic={comic}
          title={translations[`comic.${comic.id}.title`]}
          author={translations[`comic.${comic.id}.author`]}
          featuredLabel={tr('Nổi bật', 'Featured')}
        />)}
      </div>
    </section>
  )
}

export default function HomePage() {
  const { tr } = useLanguage()
  const [banners, setBanners] = useState([fallbackBanner])
  const [newComics, setNewComics] = useState([])
  const [outstanding, setOutstanding] = useState([])
  const [current, setCurrent] = useState(0)

  const translationTexts = useMemo(() => [
    ...banners.map((item, index) => ({
      key: `banner.${item.id || index}.title`,
      value: item.title,
    })),
    ...[...newComics, ...outstanding].flatMap((comic) => [
      { key: `comic.${comic.id}.title`, value: comic.title },
      { key: `comic.${comic.id}.author`, value: comic.authorName },
    ]),
  ], [banners, newComics, outstanding])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  useEffect(() => {
    getBanners()
      .then((items) => {
        if (Array.isArray(items) && items.length) setBanners(items)
      })
      .catch(() => {})
    getNewComics(4)
      .then((data) => setNewComics(visibleComics(data.items || data || [])))
      .catch(() => {})
    getOutstandingComics(4)
      .then((data) => setOutstanding(visibleComics(data.items || data || [])))
      .catch(() => {})
  }, [])

  useEffect(() => {
    if (!localStorage.getItem('accessToken') || hasGuestRoleInToken()) return undefined
    const heartbeat = () => {
      if (document.visibilityState === 'visible') trackLobbyMinute().catch(() => {})
    }
    heartbeat()
    const timer = window.setInterval(heartbeat, 15000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    if (banners.length < 2) return undefined
    const timer = window.setInterval(
      () => setCurrent((value) => (value + 1) % banners.length),
      5000,
    )
    return () => window.clearInterval(timer)
  }, [banners.length])

  const banner = banners[current] || fallbackBanner
  const bannerTitle = translations[`banner.${banner.id || current}.title`] || banner.title
  const move = (step) =>
    setCurrent((value) => (value + step + banners.length) % banners.length)

  return (
    <div className="home-page">
      <SiteHeader />
      {translationError && <div className="global-translation-alert">{translationError}</div>}
      <main className="home-main">
        <section
          className="hero-slider"
          style={{ backgroundImage: `url("${banner.imageUrl || fallbackBanner.imageUrl}")` }}
        >
          <div className="hero-shade" />
          <button className="slider-arrow left" onClick={() => move(-1)}>‹</button>
          {(bannerTitle || banner.linkUrl) && (
            <div className="hero-content">
              {bannerTitle && <h1>{bannerTitle}</h1>}
              <p>{tr('Khám phá thế giới truyện tranh hấp dẫn trên Comico', 'Discover an exciting world of comics on Comico')}</p>
              {banner.linkUrl && <a href={banner.linkUrl} className="read-now">▶ {tr('Xem ngay', 'Read now')}</a>}
            </div>
          )}
          <button className="slider-arrow right" onClick={() => move(1)}>›</button>
        </section>

        <div className="slider-dots">
          {banners.map((item, index) => (
            <button
              key={item.id || index}
              className={index === current ? 'active' : ''}
              onClick={() => setCurrent(index)}
            />
          ))}
        </div>

        <ComicSection
          id="featured-comics"
          title={tr('★ Truyện nổi bật', '★ Featured comics')}
          items={outstanding.slice(0, 4)}
          target="/collections/featured"
          translations={translations}
          tr={tr}
        />
        <ComicSection
          title={tr('Đề xuất truyện hay', 'Recommended comics')}
          items={newComics.slice(0, 4)}
          target="/collections/recommended"
          translations={translations}
          tr={tr}
        />
        {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
      </main>
    </div>
  )
}
