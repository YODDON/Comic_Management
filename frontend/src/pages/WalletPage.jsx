import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import { getWalletSummary } from '../services/walletService'
import { useLanguage } from '../contexts/LanguageContext'

const formatCoin = (value, locale) => Number(value || 0).toLocaleString(locale)

function WalletIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 7.5h17a1 1 0 0 1 1 1v10a1.5 1.5 0 0 1-1.5 1.5h-15A1.5 1.5 0 0 1 3 18.5z" /><path d="M3 8V5.5A1.5 1.5 0 0 1 4.5 4H18v3.5" /><circle cx="17.5" cy="14" r="1" /></svg>
}

function CoinsIcon() {
  return <svg viewBox="0 0 48 48" aria-hidden="true"><ellipse cx="19" cy="12" rx="13" ry="6" /><path d="M6 12v8c0 3.3 5.8 6 13 6s13-2.7 13-6v-8M6 20v8c0 3.3 5.8 6 13 6 4 0 7.6-.9 10-2.4" /><path d="M31 18c6.3.3 11 2.8 11 5.8 0 3.2-5.8 5.9-13 5.9M29 29.7v7.5c0 3.2-5.8 5.8-13 5.8S3 40.4 3 37.2v-8" /></svg>
}

function ActionIcon({ name }) {
  if (name === 'deposit') return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 7v10M7 12h10" /></svg>
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 12a8 8 0 1 0 2.3-5.7L4 8.6" /><path d="M4 4v4.6h4.6M12 7v5l3 2" /></svg>
}

function InfoIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M12 11v6M12 7.5v.5" /></svg>
}

function LinkIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m10 14 4-4" /><path d="M8.5 16.5 7 18a3.5 3.5 0 0 1-5-5l3-3a3.5 3.5 0 0 1 5 0M15.5 7.5 17 6a3.5 3.5 0 0 1 5 5l-3 3a3.5 3.5 0 0 1-5 0" /></svg>
}

const actions = [
  { to: '/wallet/deposit', icon: 'deposit', vi: 'Nạp tiền', en: 'Deposit' },
  { to: '/wallet/transactions', icon: 'history', vi: 'Lịch sử', en: 'History' },
]

export default function WalletPage() {
  const { language, locale, tr } = useLanguage()
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const hasToken = Boolean(localStorage.getItem('accessToken'))

  useEffect(() => {
    if (!hasToken) return
    getWalletSummary()
      .then((result) => setSummary(result.data))
      .catch((err) => setError(language === 'en' ? 'Unable to load your wallet.' : err.message))
      .finally(() => setLoading(false))
  }, [hasToken, language])

  if (!hasToken) return <Navigate to="/login" replace />

  const balance = summary?.balance ?? 0
  const locked = summary?.locked ?? 0

  return (
    <div className="wallet-page">
      <SiteHeader />
      <main>
        <div className="wallet-heading wallet-heading-compact">
          <h1><WalletIcon />{tr('Ví của tôi', 'My wallet')}</h1>
        </div>

        {error && <div className="wallet-notice error"><b>!</b><span>{error}</span></div>}

        <section className="wallet-balance-card">
          <div className="wallet-balance-icon"><CoinsIcon /></div>
          <div className="wallet-balance-body">
            <small>{tr('Số dư hiện tại', 'Current balance')}</small>
            <strong>{loading ? '…' : formatCoin(balance, locale)} <span>🍓</span></strong>
            {!loading && locked > 0 && (
              <em>{tr(`${formatCoin(locked, locale)} Dâu thưởng chỉ dùng để đọc truyện`, `${formatCoin(locked, locale)} bonus Dâu can only be used to read comics`)}</em>
            )}
            {!loading && locked === 0 && <em>{tr('Dùng Dâu để mở khóa chapter có phí.', 'Use Dâu to unlock paid chapters.')}</em>}
          </div>
        </section>

        <section className="wallet-actions">
          {actions.map((action) => (
            <Link className="wallet-action" to={action.to} key={action.to}>
              <span className="wallet-action-icon"><ActionIcon name={action.icon} /></span>
              <b>{tr(action.vi, action.en)}</b>
            </Link>
          ))}
        </section>

        <section className="wallet-info-grid">
          <div className="wallet-info">
            <h3><InfoIcon />{tr('Thông tin', 'Information')}</h3>
            <ul>
              <li>{tr('Dùng Dâu trong ví để mua chương truyện có phí (1 🍓 = 1 VND).', 'Use wallet Dâu to unlock paid chapters (1 🍓 = 1 VND).')}</li>
              <li>{tr('Hoàn thành nhiệm vụ để nhận thêm Dâu thưởng.', 'Complete missions to earn bonus Dâu.')}</li>
            </ul>
          </div>
          <div className="wallet-info">
            <h3><LinkIcon />{tr('Liên kết nhanh', 'Quick links')}</h3>
            <ul className="wallet-links">
              <li><Link to="/missions"><span>♛</span>{tr('Nhiệm vụ', 'Missions')}</Link></li>
              <li><Link to="/wallet/transactions"><span>▤</span>{tr('Giao dịch', 'Transactions')}</Link></li>
              <li><Link to="/search"><span>⌕</span>{tr('Tìm truyện', 'Discover')}</Link></li>
            </ul>
          </div>
        </section>
      </main>
    </div>
  )
}
