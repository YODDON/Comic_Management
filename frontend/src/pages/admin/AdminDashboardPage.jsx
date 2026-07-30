import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { getDashboardData } from '../../services/dashboardService'
import { useLanguage } from '../../contexts/LanguageContext'

const empty = {
  comics: { total: 0, approved: 0, pending: 0, rejected: 0 },
  system: { comics: 0, categories: 0, notifications: 0, missions: 0, banners: 0 },
  finance: { transactionCount: 0, transactionAmount: 0 },
  queue: { pendingComics: 0 },
}
const money = (value, locale) => new Intl.NumberFormat(locale).format(value || 0)

function Donut({ values, colors, center, legend, totalLabel }) {
  const total = Math.max(values.reduce((sum, value) => sum + value, 0), 1)
  const angles = values.map((_, index) => values.slice(0, index + 1).reduce((sum, value) => sum + value, 0) / total * 360)
  const stops = values.map((_, index) => {
    const start = index === 0 ? 0 : angles[index - 1]
    return `${colors[index]} ${start}deg ${angles[index]}deg`
  }).join(', ')
  return <div className="dashboard-donut-wrap"><div className="dashboard-donut" style={{ background: `conic-gradient(${stops || '#ece8ed 0deg 360deg'})` }}><span><b>{center}</b><small>{totalLabel}</small></span></div><div className="dashboard-legend">{legend.map((item, index) => <span key={item}><i style={{ background: colors[index] }} />{item} <b>{values[index]}</b></span>)}</div></div>
}

function Bars({ items }) {
  const max = Math.max(...items.map((item) => item.value), 1)
  return <div className="dashboard-bars">{items.map((item) => <div className="dashboard-bar-item" key={item.label}><b>{item.value}</b><div><span style={{ height: `${Math.max(item.value / max * 100, item.value ? 8 : 0)}%`, background: item.color }} /></div><small>{item.label}</small></div>)}</div>
}

export default function AdminDashboardPage() {
  const { language, locale, tr } = useLanguage()
  const navigate = useNavigate()
  const [data, setData] = useState(empty)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    getDashboardData().then(setData).catch((requestError) => setError(language === 'en' ? 'Unable to load dashboard data.' : requestError.message)).finally(() => setLoading(false))
  }, [language])

  const statCards = useMemo(() => [
    { label: tr('Tổng truyện', 'Total comics'), value: data.comics.total, color: '#ec4899', path: '/admin/comics/approved' },
    { label: tr('Chờ duyệt', 'Pending'), value: data.comics.pending, color: '#f5b91f', path: '/admin/comics/pending' },
    { label: tr('Đã duyệt', 'Approved'), value: data.comics.approved, color: '#27c975', path: '/admin/comics/approved' },
    { label: tr('Bị từ chối', 'Rejected'), value: data.comics.rejected, color: '#ef5350', path: '/admin/comics/rejected' },
  ], [data, tr])

  return <AdminLayout title={tr('Bảng điều khiển', 'Dashboard')}><div className="dashboard-page">
    <div className="dashboard-heading"><div><h2>◉ {tr('Bảng điều khiển', 'Dashboard')}</h2><p>{tr('Tổng quan hệ thống quản trị Comico.', 'Overview of the Comico administration system.')}</p></div><button onClick={() => window.location.reload()}>↻ {tr('Làm mới', 'Refresh')}</button></div>
    {error && <div className="admin-alert error">{error}</div>}
    {loading ? <div className="dashboard-loading">{tr('Đang tổng hợp dữ liệu hệ thống...', 'Loading system data...')}</div> : <>
      <section className="dashboard-stat-grid">{statCards.map((card) => <button key={card.label} onClick={() => navigate(card.path)} style={{ '--stat-color': card.color }}><small>{card.label}</small><b>{card.value}</b><span>{tr('Xem chi tiết →', 'View details →')}</span></button>)}</section>
      <section className="dashboard-grid">
        <article className="dashboard-panel"><h3>◉ {tr('Trạng thái truyện', 'Comic status')}</h3><Donut values={[data.comics.pending, data.comics.approved, data.comics.rejected]} colors={['#f8bf21', '#31d17b', '#ef5350']} center={data.comics.total} legend={[tr('Chờ duyệt', 'Pending'), tr('Đã duyệt', 'Approved'), tr('Bị từ chối', 'Rejected')]} totalLabel={tr('Tổng', 'Total')} /></article>
        <article className="dashboard-panel"><h3>▥ {tr('Tổng quan hệ thống', 'System overview')}</h3><Bars items={[{ label: tr('Truyện', 'Comics'), value: data.system.comics, color: '#5b9df5' }, { label: tr('Thể loại', 'Genres'), value: data.system.categories, color: '#9b75ed' }, { label: tr('Thông báo', 'Notifications'), value: data.system.notifications, color: '#f3bf32' }, { label: tr('Nhiệm vụ', 'Missions'), value: data.system.missions, color: '#35c99a' }, { label: 'Banner', value: data.system.banners, color: '#ec6ca8' }]} /></article>
        <article className="dashboard-panel"><h3>▣ {tr('Tài chính', 'Finance')}</h3><div className="finance-bars"><div><label><span>{tr('Tổng giao dịch', 'Total transactions')}</span><b>{data.finance.transactionCount}</b></label><i><span style={{ width: `${Math.min(data.finance.transactionCount * 5, 100)}%` }} /></i><small>{money(data.finance.transactionAmount, locale)} Dâu</small></div></div></article>
        <article className="dashboard-panel"><h3>☷ {tr('Chờ xử lý', 'Pending actions')}</h3><Donut values={[data.queue.pendingComics]} colors={['#fbbd18']} center={data.queue.pendingComics} legend={[tr('Truyện chờ duyệt', 'Pending comics')]} totalLabel={tr('Tổng', 'Total')} /><div className="dashboard-queue-actions"><button onClick={() => navigate('/admin/comics/pending')}>{tr('Xử lý truyện', 'Review comics')}</button></div></article>
      </section>
    </>}
  </div></AdminLayout>
}
