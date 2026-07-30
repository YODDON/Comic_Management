import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import {
  getAdminUsers,
  updateAdminUserLock,
  updateAdminUserRole,
} from '../../services/adminUserService'
import { useLanguage } from '../../contexts/LanguageContext'
import { getRoleLabel } from '../../utils/roleLabels'

const formatDate = (value, locale) =>
  new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
export default function AdminUsersPage() {
  const { language, locale, tr } = useLanguage()
  const navigate = useNavigate()
  const [items, setItems] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [updatingId, setUpdatingId] = useState(null)
  const [message, setMessage] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await getAdminUsers({
        pageNumber: page,
        pageSize,
        search,
        status,
      })
      setItems(data.items || [])
      setTotal(data.totalCount || 0)
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load users.' : error.message })
    } finally {
      setLoading(false)
    }
  }, [language, page, pageSize, search, status])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  async function toggleLock(user) {
    const locking = user.isActive
    if (!window.confirm(tr(`${locking ? 'Khóa' : 'Mở khóa'} tài khoản “${user.username}”?`, `${locking ? 'Lock' : 'Unlock'} account “${user.username}”?`))) {
      return
    }

    setUpdatingId(user.id)
    try {
      await updateAdminUserLock(user.id, locking)
      setMessage({
        type: 'success',
        text: locking
          ? tr('Đã khóa tài khoản người dùng.', 'User account locked.')
          : tr('Đã mở khóa tài khoản người dùng.', 'User account unlocked.'),
      })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update the account status.' : error.message })
    } finally {
      setUpdatingId(null)
    }
  }

  async function changeRole(user, role) {
    const currentRole = user.roles?.[0] || 'Guest'
    if (role === currentRole) return
    const currentRoleLabel = getRoleLabel(currentRole)
    const roleLabel = getRoleLabel(role)

    if (!window.confirm(
      tr(`Đổi role của “${user.username}” từ ${currentRoleLabel} sang ${roleLabel}? Người dùng sẽ phải đăng nhập lại.`, `Change “${user.username}” from ${currentRoleLabel} to ${roleLabel}? The user will need to sign in again.`),
    )) {
      return
    }

    setUpdatingId(user.id)
    try {
      await updateAdminUserRole(user.id, role)
      setMessage({
        type: 'success',
        text: tr(`Đã phân quyền ${roleLabel} cho ${user.username}.`, `${roleLabel} role assigned to ${user.username}.`),
      })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update the user role.' : error.message })
    } finally {
      setUpdatingId(null)
    }
  }

  return (
    <AdminLayout title={tr('Quản lý người dùng', 'User management')}>
      <div className="admin-page-heading">
        <div>
          <h2>♟ {tr('Quản lý người dùng', 'User management')}</h2>
          <p>{tr('Xem thông tin, phân quyền User/Guest và khóa tài khoản.', 'View details, assign User/Guest roles and lock accounts.')}</p>
        </div>
      </div>

      {message && (
        <div className={`admin-alert ${message.type}`}>
          {message.text}
          <button onClick={() => setMessage(null)}>×</button>
        </div>
      )}

      <form
        className="admin-filters admin-list-filters user-admin-filters"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
          setSearch(searchInput.trim())
        }}
      >
        <input
          placeholder={tr('Tìm theo tên hoặc email...', 'Search by name or email...')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="">{tr('Tất cả trạng thái', 'All statuses')}</option>
          <option value="true">{tr('Đang hoạt động', 'Active')}</option>
          <option value="false">{tr('Đã bị khóa', 'Locked')}</option>
        </select>
        <PageSizeSelect value={pageSize} onChange={(value) => { setPageSize(value); setPage(1) }} />
        <button className="search-button">⌕ {tr('Tìm', 'Search')}</button>
        <button
          type="button"
          className="reset-button"
          onClick={() => {
            setSearchInput('')
            setSearch('')
            setStatus('')
            setPage(1)
          }}
        >
          {tr('Đặt lại', 'Reset')}
        </button>
      </form>

      <section className="admin-table-card">
        <h3>{tr('Danh sách người dùng', 'User list')} ({total})</h3>
        <div className="admin-table-wrap">
          <table className="user-admin-table">
            <thead>
              <tr>
                <th>{tr('NGƯỜI DÙNG', 'USER')}</th>
                <th>{tr('VAI TRÒ', 'ROLE')}</th>
                <th>{tr('XÁC THỰC EMAIL', 'EMAIL VERIFICATION')}</th>
                <th>{tr('TRẠNG THÁI', 'STATUS')}</th>
                <th>{tr('NGÀY TẠO', 'CREATED')}</th>
                <th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan="6" className="empty-cell">{tr('Đang tải người dùng...', 'Loading users...')}</td>
                </tr>
              )}
              {!loading && items.length === 0 && (
                <tr>
                  <td colSpan="6" className="empty-cell">{tr('Không tìm thấy người dùng.', 'No users found.')}</td>
                </tr>
              )}
              {!loading && items.map((user) => (
                <tr key={user.id}>
                  <td>
                    <div className="admin-user-cell">
                      <span>
                        {user.avatarUrl
                          ? <img src={user.avatarUrl} alt="" />
                          : user.username?.[0]?.toUpperCase()}
                      </span>
                      <div>
                        <b>{user.username}</b>
                        <small>{user.email}</small>
                      </div>
                    </div>
                  </td>
                  <td>
                    <select
                      className="user-role-select"
                      value={user.roles?.[0] || 'Guest'}
                      disabled={updatingId === user.id}
                      onChange={(event) => changeRole(user, event.target.value)}
                      aria-label={tr(`Phân quyền cho ${user.username}`, `Assign role to ${user.username}`)}
                    >
                      <option value="Reader">User</option>
                      <option value="Guest">Guest</option>
                    </select>
                  </td>
                  <td>
                    <span className={`user-verify-badge ${user.isEmailVerified ? 'verified' : ''}`}>
                      {user.isEmailVerified ? tr('Đã xác thực', 'Verified') : tr('Chưa xác thực', 'Not verified')}
                    </span>
                  </td>
                  <td>
                    <span className={`user-lock-badge ${user.isActive ? 'active' : 'locked'}`}>
                      {user.isActive ? tr('Hoạt động', 'Active') : tr('Đã khóa', 'Locked')}
                    </span>
                  </td>
                  <td>{formatDate(user.createdAt, locale)}</td>
                  <td>
                    <div className="row-actions">
                      <button
                        className="edit"
                        onClick={() => navigate(`/admin/users/${user.id}`)}
                      >
                        {tr('Xem chi tiết', 'View details')}
                      </button>
                      <button
                        className={user.isActive ? 'delete' : 'unlock'}
                        disabled={updatingId === user.id}
                        onClick={() => toggleLock(user)}
                      >
                        {user.isActive ? tr('Khóa', 'Lock') : tr('Mở khóa', 'Unlock')}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={page}
          totalItems={total}
          pageSize={pageSize}
          onChange={setPage}
        />
      </section>
    </AdminLayout>
  )
}
