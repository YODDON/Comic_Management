import { useCallback, useEffect, useState } from 'react'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import { getAdminWithdraws, updateWithdrawStatus, WITHDRAW_STATUS } from '../../services/walletService'
import { useLanguage } from '../../contexts/LanguageContext'

const formatCoin = (value, locale) => Number(value || 0).toLocaleString(locale)
const STATUS_META = {
  Pending: { vi: 'Chờ duyệt', en: 'Pending', cls: 'pending' },
  Approved: { vi: 'Đã duyệt', en: 'Approved', cls: 'approved' },
  Rejected: { vi: 'Từ chối', en: 'Rejected', cls: 'rejected' },
}

export default function AdminWithdrawalsPage() {
  const { language, locale, tr } = useLanguage()
  const [items, setItems] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState(null)
  const [acting, setActing] = useState(null)
  const [rejecting, setRejecting] = useState(null)
  const [note, setNote] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await getAdminWithdraws(status, page, pageSize, search)
      setItems(result.data?.items || [])
      setTotalCount(result.data?.totalCount || 0)
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load withdrawal requests.' : error.message })
    } finally {
      setLoading(false)
    }
  }, [language, status, page, pageSize, search])

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
    setStatus('')
    setPageSize(10)
    setPage(1)
  }

  async function approve(item) {
    if (!window.confirm(
      tr(`Duyệt yêu cầu rút ${formatCoin(item.amount, locale)} Dâu của “${item.accountName}”?\n\nHệ thống không tự chuyển tiền. Bạn phải chuyển khoản thủ công ${formatCoin(item.amount, locale)}đ tới ${item.bankName} - ${item.bankAccount} sau khi duyệt.`, `Approve the ${formatCoin(item.amount, locale)} Dâu withdrawal for “${item.accountName}”?\n\nThe system does not transfer money automatically. After approval, manually transfer ${formatCoin(item.amount, locale)} VND to ${item.bankName} - ${item.bankAccount}.`),
    )) return

    setActing(item.id)
    try {
      await updateWithdrawStatus(item.id, WITHDRAW_STATUS.APPROVED)
      setMessage({
        type: 'success',
        text: tr(`Đã duyệt. Nhớ chuyển khoản thủ công cho ${item.accountName}.`, `Approved. Remember to transfer the money manually to ${item.accountName}.`),
      })
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to approve the withdrawal.' : error.message })
    } finally {
      setActing(null)
      await load()
    }
  }

  async function confirmReject(event) {
    event.preventDefault()
    const item = rejecting
    setActing(item.id)
    try {
      await updateWithdrawStatus(item.id, WITHDRAW_STATUS.REJECTED, note.trim())
      setMessage({
        type: 'success',
        text: tr('Đã từ chối. Số Dâu đã được hoàn lại vào ví người dùng.', 'Rejected. The Dâu amount has been refunded to the user wallet.'),
      })
      setRejecting(null)
      setNote('')
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to reject the withdrawal.' : error.message })
    } finally {
      setActing(null)
      await load()
    }
  }

  function openReject(item) {
    setRejecting(item)
    setNote('')
  }

  return (
    <AdminLayout title={tr('Quản lý rút tiền', 'Withdrawal management')}>
      <div className="admin-page-heading">
        <div>
          <h2><span>●</span> {tr('Quản lý rút tiền', 'Withdrawal management')}</h2>
          <p>{tr('Duyệt hoặc từ chối yêu cầu rút Dâu thành tiền mặt.', 'Approve or reject requests to withdraw Dâu as cash.')}</p>
        </div>
      </div>

      <div className="admin-alert warn">
        {tr('Hệ thống', 'The system does')} <b>{tr('không', 'not')}</b> {tr('tự chuyển tiền. Sau khi bấm', 'transfer money automatically. After selecting')} <b>{tr('Duyệt', 'Approve')}</b>, {tr('bạn phải chuyển khoản thủ công cho người dùng.', 'you must transfer the money to the user manually.')}
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
          placeholder={tr('Tìm User ID, chủ tài khoản hoặc số tài khoản...', 'Search User ID, account holder or account number...')}
        />
        <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">{tr('Tất cả trạng thái', 'All statuses')}</option>
          <option value={WITHDRAW_STATUS.PENDING}>{tr('Chờ duyệt', 'Pending')}</option>
          <option value={WITHDRAW_STATUS.APPROVED}>{tr('Đã duyệt', 'Approved')}</option>
          <option value={WITHDRAW_STATUS.REJECTED}>{tr('Từ chối', 'Rejected')}</option>
        </select>
        <PageSizeSelect
          value={pageSize}
          onChange={(value) => { setPageSize(value); setPage(1) }}
        />
        <button className="search-button" type="submit">⌕ {tr('Tìm', 'Search')}</button>
        <button className="reset-button" type="button" onClick={resetFilters}>{tr('Đặt lại', 'Reset')}</button>
      </form>

      <section className="admin-table-card">
        <h3>{tr('Yêu cầu rút tiền', 'Withdrawal requests')} ({totalCount})</h3>
        <div className="admin-table-wrap">
          <table>
            <thead>
              <tr>
                <th>{tr('CHỦ TÀI KHOẢN', 'ACCOUNT HOLDER')}</th>
                <th>{tr('NGÂN HÀNG', 'BANK')}</th>
                <th>{tr('SỐ TÀI KHOẢN', 'ACCOUNT NUMBER')}</th>
                <th>{tr('SỐ TIỀN', 'AMOUNT')}</th>
                <th>{tr('NGÀY TẠO', 'CREATED')}</th>
                <th>{tr('TRẠNG THÁI', 'STATUS')}</th>
                <th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
              </tr>
            </thead>
            <tbody>
              {loading && <tr><td colSpan="7" className="empty-cell">{tr('Đang tải...', 'Loading...')}</td></tr>}
              {!loading && items.length === 0 && (
                <tr><td colSpan="7" className="empty-cell">{tr('Không tìm thấy yêu cầu rút tiền phù hợp.', 'No matching withdrawal requests found.')}</td></tr>
              )}
              {!loading && items.map((withdrawal) => {
                const meta = STATUS_META[withdrawal.status]
                  || { vi: withdrawal.status, en: withdrawal.status, cls: 'pending' }
                const isPending = withdrawal.status === WITHDRAW_STATUS.PENDING
                return (
                  <tr key={withdrawal.id}>
                    <td><b>{withdrawal.accountName || '—'}</b></td>
                    <td>{withdrawal.bankName}</td>
                    <td className="wd-mono">{withdrawal.bankAccount}</td>
                    <td>
                      <b>{formatCoin(withdrawal.amount, locale)} Dâu</b>
                      <small className="wd-sub">
                        {tr('phí', 'fee')} {formatCoin(withdrawal.fee, locale)} · {tr('trừ ví', 'wallet deduction')} {formatCoin(withdrawal.totalDeducted, locale)}
                      </small>
                    </td>
                    <td>{new Date(withdrawal.createdAt).toLocaleString(locale)}</td>
                    <td>
                      <span className={`wd-status ${meta.cls}`}>{tr(meta.vi, meta.en)}</span>
                      {withdrawal.note && <small className="wd-sub">{withdrawal.note}</small>}
                    </td>
                    <td>
                      {isPending ? (
                        <div className="row-actions">
                          <button
                            className="approve"
                            disabled={acting === withdrawal.id}
                            onClick={() => approve(withdrawal)}
                          >
                            {tr('Duyệt', 'Approve')}
                          </button>
                          <button
                            className="delete"
                            disabled={acting === withdrawal.id}
                            onClick={() => openReject(withdrawal)}
                          >
                            {tr('Từ chối', 'Reject')}
                          </button>
                        </div>
                      ) : (
                        <span className="wd-processed">
                          {withdrawal.processedAt
                            ? new Date(withdrawal.processedAt).toLocaleDateString(locale)
                            : '—'}
                        </span>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
        <Pagination page={page} totalItems={totalCount} pageSize={pageSize} onChange={setPage} />
      </section>

      {rejecting && (
        <div className="admin-modal-backdrop" onMouseDown={() => !acting && setRejecting(null)}>
          <form
            className="admin-modal wd-reject-modal"
            onSubmit={confirmReject}
            onMouseDown={(event) => event.stopPropagation()}
          >
            <div>
              <h3>{tr('Từ chối yêu cầu rút', 'Reject withdrawal request')}</h3>
              <button type="button" onClick={() => setRejecting(null)}>×</button>
            </div>
            <p className="wd-reject-info">
              {tr('Rút', 'Withdrawal of')} <b>{formatCoin(rejecting.amount, locale)} Dâu</b> {tr('của', 'for')} <b>{rejecting.accountName}</b>.
              {' '}{tr('Số Dâu đã trừ sẽ được hoàn lại vào ví.', 'The deducted Dâu will be refunded to the wallet.')}
            </p>
            <label>
              {tr('Lý do từ chối', 'Rejection reason')} <span className="wd-req">*</span>
              <textarea
                required
                maxLength="1000"
                value={note}
                placeholder={tr('Ví dụ: Thông tin tài khoản không khớp', 'Example: The account details do not match')}
                onChange={(event) => setNote(event.target.value)}
              />
            </label>
            <footer>
              <button type="button" onClick={() => setRejecting(null)} disabled={acting}>{tr('Hủy', 'Cancel')}</button>
              <button className="save wd-reject-submit" disabled={acting || !note.trim()}>
                {acting ? tr('Đang xử lý...', 'Processing...') : tr('Xác nhận từ chối', 'Confirm rejection')}
              </button>
            </footer>
          </form>
        </div>
      )}
    </AdminLayout>
  )
}
