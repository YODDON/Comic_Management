import { useEffect, useMemo, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import { getTransactions, TX_TYPE, TX_STATUS } from '../services/walletService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

const formatCoin = (value, locale) => Number(value || 0).toLocaleString(locale)
const PAGE_SIZE = 10

const TYPES = {
  [TX_TYPE.PURCHASE]: { vi: 'Mở khóa chapter', en: 'Chapter unlocked', sign: -1 },
  [TX_TYPE.MANUAL_TOPUP]: { vi: 'Nạp Dâu', en: 'Dâu deposit', sign: +1 },
  [TX_TYPE.WITHDRAW]: { vi: 'Rút Dâu', en: 'Dâu withdrawal', sign: -1 },
  [TX_TYPE.REFUND]: { vi: 'Hoàn Dâu', en: 'Dâu refund', sign: +1 },
  [TX_TYPE.MISSION_REWARD]: { vi: 'Thưởng nhiệm vụ', en: 'Mission reward', sign: +1 },
  [TX_TYPE.FEE]: { vi: 'Phí giao dịch', en: 'Transaction fee', sign: -1 },
}

const STATUSES = {
  [TX_STATUS.PENDING]: { vi: 'Đang xử lý', en: 'Pending', cls: 'pending' },
  [TX_STATUS.COMPLETED]: { vi: 'Thành công', en: 'Completed', cls: 'done' },
  [TX_STATUS.REJECTED]: { vi: 'Từ chối', en: 'Rejected', cls: 'bad' },
  [TX_STATUS.FAILED]: { vi: 'Thất bại', en: 'Failed', cls: 'bad' },
}

function TransactionDescription({ transaction, type, translatedChapterTitle, tr }) {
  if (transaction.type !== TX_TYPE.PURCHASE) {
    return <b className={`history-type ${type.sign > 0 ? 'in' : 'out'}`}>{tr(type.vi, type.en)}</b>
  }

  const chapterTitle = translatedChapterTitle || transaction.chapterTitle
  const chapterName = transaction.chapterNumber != null
    ? tr(`Chương ${transaction.chapterNumber}: ${chapterTitle || 'Không có tiêu đề'}`, `Chapter ${transaction.chapterNumber}: ${chapterTitle || 'Untitled'}`)
    : chapterTitle || tr('Chapter đã mở khóa', 'Unlocked chapter')
  const canOpen = transaction.comicId && transaction.chapterSlug

  return (
    <span className="history-chapter">
      <b>{tr(type.vi, type.en)}</b>
      {canOpen
        ? <Link to={`/read/${transaction.comicId}/${transaction.chapterSlug}`}>{chapterName}</Link>
        : <small>{chapterName}</small>}
    </span>
  )
}

export default function TransactionHistoryPage() {
  const { language, locale, tr } = useLanguage()
  const hasToken = Boolean(localStorage.getItem('accessToken'))
  const [items, setItems] = useState([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const translationTexts = useMemo(() => items
    .filter((transaction) => transaction.type === TX_TYPE.PURCHASE && transaction.chapterTitle)
    .map((transaction) => ({ key: `transaction.${transaction.id}.chapter`, value: transaction.chapterTitle })), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  useEffect(() => {
    if (!hasToken) return undefined

    const loadTask = window.setTimeout(() => {
      setLoading(true)
      setError('')
      getTransactions(page, PAGE_SIZE)
        .then((result) => {
          setItems(result.data?.items || [])
          setTotalPages(result.data?.totalPages || 1)
        })
        .catch((requestError) => setError(language === 'en' ? 'Unable to load transaction history.' : requestError.message))
        .finally(() => setLoading(false))
    }, 0)

    return () => window.clearTimeout(loadTask)
  }, [hasToken, language, page])

  if (!hasToken) return <Navigate to="/login" replace />

  return (
    <div className="history-page">
      <SiteHeader />
      <main>
        <div className="wallet-heading">
          <p><Link to="/wallet">← {tr('Ví của tôi', 'My wallet')}</Link></p>
          <h1>📋 {tr('Lịch sử giao dịch', 'Transaction history')}</h1>
          <span>{tr('Lịch sử nạp Dâu, mở khóa chapter và nhận thưởng.', 'Your Dâu deposits, chapter unlocks and rewards.')}</span>
        </div>

        {translationError && <div className="global-translation-alert">{translationError}</div>}
        {error && <div className="wallet-notice error"><b>!</b><span>{error}</span></div>}
        {loading && <div className="history-empty">{tr('Đang tải lịch sử giao dịch...', 'Loading transaction history...')}</div>}
        {!loading && !error && items.length === 0 && (
          <div className="history-empty">{tr('Bạn chưa có giao dịch nào.', 'You do not have any transactions yet.')}</div>
        )}

        {!loading && items.length > 0 && (
          <div className="history-table">
            <div className="history-row history-head">
              <span>{tr('Ngày giờ', 'Date and time')}</span><span>{tr('Nội dung', 'Description')}</span><span>{tr('Số Dâu', 'Amount')}</span><span>{tr('Trạng thái', 'Status')}</span>
            </div>
            {items.map((transaction) => {
              const type = TYPES[transaction.type] || { vi: 'Giao dịch', en: 'Transaction', sign: +1 }
              const status = STATUSES[transaction.status] || {
                vi: String(transaction.status),
                en: String(transaction.status),
                cls: 'pending',
              }
              return (
                <div className="history-row" key={transaction.id}>
                  <span>{new Date(transaction.createdAt).toLocaleString(locale)}</span>
                  <TransactionDescription transaction={transaction} type={type} translatedChapterTitle={translations[`transaction.${transaction.id}.chapter`]} tr={tr} />
                  <span className={`history-amount ${type.sign > 0 ? 'in' : 'out'}`}>
                    {type.sign > 0 ? '+' : '−'}{formatCoin(transaction.amount, locale)} Dâu
                  </span>
                  <span><em className={`history-status ${status.cls}`}>{tr(status.vi, status.en)}</em></span>
                </div>
              )
            })}
          </div>
        )}

        {!loading && totalPages > 1 && (
          <div className="history-pager">
            <button disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
              ← {tr('Trước', 'Previous')}
            </button>
            <span>{tr('Trang', 'Page')} {page} / {totalPages}</span>
            <button disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>
              {tr('Sau', 'Next')} →
            </button>
          </div>
        )}
        {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
      </main>
    </div>
  )
}
