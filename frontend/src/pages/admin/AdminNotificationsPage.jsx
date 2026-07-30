import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import {
  deleteAdminNotification,
  getAdminNotifications,
  updateAdminNotification,
} from '../../services/notificationService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

const formatDate = (value, locale) => new Intl.DateTimeFormat(locale, {
  dateStyle: 'short',
  timeStyle: 'short',
}).format(new Date(value))

export default function AdminNotificationsPage() {
  const { language, locale, tr } = useLanguage()
  const navigate = useNavigate()
  const location = useLocation()
  const [items, setItems] = useState([])
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [message, setMessage] = useState(
    location.state?.message ? { type: 'success', text: location.state.message } : null,
  )
  const translationTexts = useMemo(() => items.flatMap((item) => [
    { key: `notification.${item.id}.title`, value: item.title },
    { key: `notification.${item.id}.body`, value: item.body },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  const load = useCallback(async () => {
    try {
      const result = await getAdminNotifications()
      setItems(result.data || [])
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load notifications.' : error.message })
    }
  }, [language])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  useEffect(() => {
    if (location.state?.message) window.history.replaceState({}, document.title)
  }, [location.state])

  const filteredItems = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase('vi-VN')
    return items.filter((item) => {
      const matchesSearch = !keyword
        || item.title?.toLocaleLowerCase('vi-VN').includes(keyword)
        || item.body?.toLocaleLowerCase('vi-VN').includes(keyword)
      const matchesStatus = status === 'all'
        || (status === 'active' ? item.isActive : !item.isActive)
      return matchesSearch && matchesStatus
    })
  }, [items, search, status])

  const lastPage = Math.max(1, Math.ceil(filteredItems.length / pageSize))
  const safePage = Math.min(page, lastPage)
  const pagedItems = filteredItems.slice((safePage - 1) * pageSize, safePage * pageSize)

  function submitFilters(event) {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  function resetFilters() {
    setSearchInput('')
    setSearch('')
    setStatus('all')
    setPageSize(10)
    setPage(1)
  }

  async function remove(item) {
    if (!window.confirm(tr(`Xóa thông báo “${item.title}”?`, `Delete notification “${translations[`notification.${item.id}.title`] || item.title}”?`))) return
    try {
      await deleteAdminNotification(item.id)
      setMessage({ type: 'success', text: tr('Đã xóa thông báo.', 'Notification deleted.') })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to delete the notification.' : error.message })
    }
  }

  async function toggle(item) {
    try {
      await updateAdminNotification(item.id, {
        title: item.title,
        body: item.body,
        isActive: !item.isActive,
      })
      setMessage({
        type: 'success',
        text: item.isActive ? tr('Đã ẩn thông báo.', 'Notification hidden.') : tr('Đã hiển thị thông báo.', 'Notification displayed.'),
      })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update the notification status.' : error.message })
    }
  }

  return (
    <AdminLayout title={tr('Quản lý thông báo', 'Notification management')}>
      <div className="admin-page-heading">
        <div>
          <h2><span>◆</span> {tr('Quản lý thông báo', 'Notification management')}</h2>
          <p>{tr('Tạo và quản lý thông báo gửi đến người dùng.', 'Create and manage notifications sent to users.')}</p>
        </div>
        <button onClick={() => navigate('/admin/notifications/create')}>＋ {tr('Thêm thông báo', 'Add notification')}</button>
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
          placeholder={tr('Tìm theo tiêu đề hoặc nội dung...', 'Search by title or content...')}
        />
        <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="all">{tr('Tất cả trạng thái', 'All statuses')}</option>
          <option value="active">{tr('Đang hiển thị', 'Visible')}</option>
          <option value="inactive">{tr('Đã ẩn', 'Hidden')}</option>
        </select>
        <PageSizeSelect
          value={pageSize}
          onChange={(value) => { setPageSize(value); setPage(1) }}
        />
        <button className="search-button" type="submit">⌕ {tr('Tìm', 'Search')}</button>
        <button className="reset-button" type="button" onClick={resetFilters}>{tr('Đặt lại', 'Reset')}</button>
      </form>

      <section className="admin-table-card notification-admin-list">
        <h3>{tr('Danh sách thông báo', 'Notification list')} ({filteredItems.length})</h3>
        <div className="admin-table-wrap">
          <table>
            <thead>
              <tr>
                <th>{tr('TIÊU ĐỀ', 'TITLE')}</th>
                <th>{tr('NỘI DUNG', 'CONTENT')}</th>
                <th>{tr('TRẠNG THÁI', 'STATUS')}</th>
                <th>{tr('THỜI GIAN', 'TIME')}</th>
                <th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
              </tr>
            </thead>
            <tbody>
              {pagedItems.length === 0 ? (
                <tr><td colSpan="5" className="empty-cell">{tr('Không tìm thấy thông báo phù hợp.', 'No matching notifications found.')}</td></tr>
              ) : pagedItems.map((item) => (
                <tr key={item.id}>
                  <td><b>{translations[`notification.${item.id}.title`] || item.title}</b></td>
                  <td>{translations[`notification.${item.id}.body`] || item.body}</td>
                  <td>
                    <button
                      className={`notification-status ${item.isActive ? 'active' : ''}`}
                      onClick={() => toggle(item)}
                    >
                      {item.isActive ? tr('Đang hiển thị', 'Visible') : tr('Đã ẩn', 'Hidden')}
                    </button>
                  </td>
                  <td>{formatDate(item.createdAt, locale)}</td>
                  <td>
                    <div className="row-actions">
                      <button className="edit" onClick={() => navigate(`/admin/notifications/${item.id}/edit`)}>{tr('Sửa', 'Edit')}</button>
                      <button className="delete" onClick={() => remove(item)}>{tr('Xóa', 'Delete')}</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={safePage}
          totalItems={filteredItems.length}
          pageSize={pageSize}
          onChange={setPage}
        />
      </section>{translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </AdminLayout>
  )
}
