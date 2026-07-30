import { useEffect, useMemo, useState } from 'react'
import { Navigate } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import Pagination from '../components/Pagination'
import { getNotifications } from '../services/notificationService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

const PAGE_SIZE = 5

export default function NotificationsPage() {
  const { language, locale, tr } = useLanguage()
  const hasToken = Boolean(localStorage.getItem('accessToken'))
  const [items, setItems] = useState([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const translationTexts = useMemo(() => items.flatMap((item) => [
    { key: `notification.${item.id}.title`, value: item.title },
    { key: `notification.${item.id}.body`, value: item.body },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  useEffect(() => {
    if (!hasToken) return undefined
    let active = true
    const load = (showError = true) => getNotifications({ page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (!active) return
        setItems(result.data?.items || [])
        setTotal(result.data?.totalCount || 0)
      })
      .catch((requestError) => {
        if (active && showError) setError(language === 'en' ? 'Unable to load notifications.' : requestError.message)
      })
      .finally(() => { if (active) setLoading(false) })
    load()
    const timer = window.setInterval(() => load(false), 15000)
    return () => {
      active = false
      window.clearInterval(timer)
    }
  }, [hasToken, language, page])

  if (!hasToken) return <Navigate to="/login" replace />

  return (
    <div className="notifications-page">
      <SiteHeader />
      <main>
        {translationError && <div className="global-translation-alert">{translationError}</div>}
        <div className="notification-title">
          <p>{tr('TÀI KHOẢN CỦA TÔI', 'MY ACCOUNT')}</p>
          <h1>{tr('Thông báo', 'Notifications')}</h1>
          <span>{tr('Cập nhật mới nhất từ Comico.', 'The latest updates from Comico.')}</span>
        </div>
        {error && <div className="mission-notice error"><b>!</b><span>{error}</span><button onClick={() => setError('')}>×</button></div>}
        {loading && <div className="notification-empty">{tr('Đang tải thông báo...', 'Loading notifications...')}</div>}
        {!loading && items.length === 0 && <div className="notification-empty">{tr('Bạn chưa có thông báo nào.', 'You do not have any notifications yet.')}</div>}
        {!loading && items.length > 0 && (
          <section className="notification-list">
            {items.map((item) => (
              <article key={item.id}>
                <span className="notification-icon">🔔</span>
                <div>
                  <h2>{translations[`notification.${item.id}.title`] || item.title}</h2>
                  <p>{translations[`notification.${item.id}.body`] || item.body}</p>
                  <time>{new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.createdAt))}</time>
                </div>
              </article>
            ))}
          </section>
        )}
        {!loading && <Pagination page={page} totalItems={total} pageSize={PAGE_SIZE} onChange={setPage} />}
        {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
      </main>
    </div>
  )
}
