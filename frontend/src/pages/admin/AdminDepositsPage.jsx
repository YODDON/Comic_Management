import { useCallback, useEffect, useState } from 'react'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import { getAdminDeposits, TX_STATUS } from '../../services/walletService'
import { useLanguage } from '../../contexts/LanguageContext'

const formatCoin = (value, locale) => Number(value || 0).toLocaleString(locale)
const STATUS_META = {
  [TX_STATUS.PENDING]: { vi: 'Chờ webhook', en: 'Waiting for webhook', cls: 'pending' },
  [TX_STATUS.COMPLETED]: { vi: 'Thành công', en: 'Completed', cls: 'done' },
  [TX_STATUS.REJECTED]: { vi: 'Từ chối', en: 'Rejected', cls: 'bad' },
  [TX_STATUS.FAILED]: { vi: 'Thất bại', en: 'Failed', cls: 'bad' },
}

export default function AdminDepositsPage() {
  const { language, locale, tr } = useLanguage()
  const [items, setItems] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await getAdminDeposits(page, pageSize, statusFilter, search)
      setItems(result.data?.items || [])
      setTotalCount(result.data?.totalCount || 0)
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load deposit transactions.' : error.message })
    } finally {
      setLoading(false)
    }
  }, [language, page, pageSize, search, statusFilter])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  function submitFilters(event) {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  function resetFilters() {
    setSearchInput('')
    setSearch('')
    setStatusFilter('')
    setPageSize(10)
    setPage(1)
  }

  return (
    <AdminLayout title={tr('Quản lý nạp tiền', 'Deposit management')}>
      <div className="admin-page-heading">
        <div>
          <h2><span>$</span> {tr('Quản lý nạp tiền', 'Deposit management')}</h2>
          <p>{tr('Theo dõi giao dịch nạp Dâu — chỉ xem, không phê duyệt.', 'Monitor Dâu deposits — view only, no manual approval.')}</p>
        </div>
      </div>

      <div className="admin-alert warn">
        {tr('Nạp tiền', 'Deposits are processed')} <b>{tr('tự động', 'automatically')}</b> {tr('qua webhook SePay, không cần admin duyệt. Giao dịch “Chờ webhook” là giao dịch chưa nhận được tiền; hãy đối soát bằng', 'through the SePay webhook and do not require admin approval. “Waiting for webhook” means the payment has not arrived; reconcile it using the')} <b>{tr('Mã giao dịch', 'transaction code')}</b>.
      </div>
      {message && (
        <div className={`admin-alert ${message.type}`}>
          {message.text}
          <button onClick={() => setMessage(null)}>×</button>
        </div>
      )}
      <form className="admin-filters admin-list-filters" onSubmit={submitFilters}>
        <input
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder={tr('Tìm mã giao dịch hoặc User ID...', 'Search transaction code or User ID...')}
        />
        <select
          value={statusFilter}
          onChange={(event) => { setStatusFilter(event.target.value); setPage(1) }}
        >
          <option value="">{tr('Tất cả trạng thái', 'All statuses')}</option>
          <option value={TX_STATUS.PENDING}>{tr('Chờ webhook', 'Waiting for webhook')}</option>
          <option value={TX_STATUS.COMPLETED}>{tr('Thành công', 'Completed')}</option>
          <option value={TX_STATUS.REJECTED}>{tr('Từ chối', 'Rejected')}</option>
          <option value={TX_STATUS.FAILED}>{tr('Thất bại', 'Failed')}</option>
        </select>
        <PageSizeSelect
          value={pageSize}
          onChange={(value) => { setPageSize(value); setPage(1) }}
        />
        <button className="search-button" type="submit">⌕ {tr('Tìm', 'Search')}</button>
        <button className="reset-button" type="button" onClick={resetFilters}>{tr('Đặt lại', 'Reset')}</button>
      </form>

      <section className="admin-table-card">
        <h3>{tr('Giao dịch nạp', 'Deposit transactions')} ({totalCount})</h3>
        <div className="admin-table-wrap">
          <table>
            <thead>
              <tr>
                <th>USER</th>
                <th>{tr('LOẠI', 'TYPE')}</th>
                <th>{tr('MÃ GIAO DỊCH', 'TRANSACTION CODE')}</th>
                <th>{tr('NGÀY', 'DATE')}</th>
                <th>{tr('SỐ TIỀN', 'AMOUNT')}</th>
                <th>{tr('TRẠNG THÁI', 'STATUS')}</th>
              </tr>
            </thead>
            <tbody>
              {loading && <tr><td colSpan="6" className="empty-cell">{tr('Đang tải...', 'Loading...')}</td></tr>}
              {!loading && items.length === 0 && (
                <tr><td colSpan="6" className="empty-cell">{tr('Không tìm thấy giao dịch nạp phù hợp.', 'No matching deposit transactions found.')}</td></tr>
              )}
              {!loading && items.map((transaction) => {
                const meta = STATUS_META[transaction.status]
                  || { vi: String(transaction.status), en: String(transaction.status), cls: 'pending' }
                return (
                  <tr key={transaction.id}>
                    <td><b>#{transaction.userId}</b></td>
                    <td>{tr('Nạp tiền', 'Deposit')}</td>
                    <td className="dep-code">{transaction.transactionCode || '—'}</td>
                    <td>{new Date(transaction.createdAt).toLocaleString(locale)}</td>
                    <td><b>{formatCoin(transaction.amount, locale)} Dâu</b></td>
                    <td><span className={`dep-status ${meta.cls}`}>{tr(meta.vi, meta.en)}</span></td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <Pagination
          page={page}
          totalItems={totalCount}
          pageSize={pageSize}
          onChange={setPage}
        />
      </section>
    </AdminLayout>
  )
}
