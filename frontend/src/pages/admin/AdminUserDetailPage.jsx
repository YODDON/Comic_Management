import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import {
  getAdminUser,
  updateAdminUserLock,
  updateAdminUserRole,
} from '../../services/adminUserService'
import { useLanguage } from '../../contexts/LanguageContext'
import { getRoleLabel } from '../../utils/roleLabels'

const formatDate = (value, locale, emptyLabel) =>
  value
    ? new Intl.DateTimeFormat(locale, {
        dateStyle: 'long',
        timeStyle: 'short',
      }).format(new Date(value))
    : emptyLabel

export default function AdminUserDetailPage() {
  const { language, locale, tr } = useLanguage()
  const { id } = useParams()
  const navigate = useNavigate()
  const [user, setUser] = useState(null)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    getAdminUser(id)
      .then(setUser)
      .catch((requestError) => setError(language === 'en' ? 'Unable to load the user.' : requestError.message))
  }, [id, language])

  async function toggleLock() {
    const locking = user.isActive
    if (!window.confirm(tr(`${locking ? 'Khóa' : 'Mở khóa'} tài khoản “${user.username}”?`, `${locking ? 'Lock' : 'Unlock'} account “${user.username}”?`))) {
      return
    }

    setSaving(true)
    setError('')
    setMessage('')
    try {
      setUser(await updateAdminUserLock(user.id, locking))
      setMessage(locking ? tr('Đã khóa tài khoản.', 'Account locked.') : tr('Đã mở khóa tài khoản.', 'Account unlocked.'))
    } catch (requestError) {
      setError(language === 'en' ? 'Unable to update the account status.' : requestError.message)
    } finally {
      setSaving(false)
    }
  }

  async function changeRole(role) {
    const currentRole = user.roles?.[0] || 'Guest'
    if (role === currentRole) return
    const currentRoleLabel = getRoleLabel(currentRole)
    const roleLabel = getRoleLabel(role)

    if (!window.confirm(
      tr(`Đổi role từ ${currentRoleLabel} sang ${roleLabel}? Người dùng sẽ phải đăng nhập lại.`, `Change the role from ${currentRoleLabel} to ${roleLabel}? The user will need to sign in again.`),
    )) {
      return
    }

    setSaving(true)
    setError('')
    setMessage('')
    try {
      setUser(await updateAdminUserRole(user.id, role))
      setMessage(tr(`Đã phân quyền ${roleLabel} cho người dùng.`, `${roleLabel} role assigned to the user.`))
    } catch (requestError) {
      setError(language === 'en' ? 'Unable to update the user role.' : requestError.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <AdminLayout title={tr('Chi tiết người dùng', 'User details')}>
      <div className="admin-user-detail-page">
        <div className="admin-page-heading">
          <div>
            <h2>{tr('Chi tiết người dùng', 'User details')}</h2>
            <p>{tr('Xem thông tin, phân quyền User/Guest và thay đổi trạng thái tài khoản.', 'View details, assign User/Guest roles and update the account status.')}</p>
          </div>
          <button onClick={() => navigate('/admin/users')}>← {tr('Danh sách', 'List')}</button>
        </div>

        {error && <div className="admin-alert error">{error}</div>}
        {message && <div className="admin-alert success">{message}</div>}

        {user ? (
          <section className="admin-user-detail-card">
            <header>
              <span>
                {user.avatarUrl
                  ? <img src={user.avatarUrl} alt="" />
                  : user.username?.[0]?.toUpperCase()}
              </span>
              <div>
                <h3>{user.username}</h3>
                <p>{user.email}</p>
                <em className={`user-lock-badge ${user.isActive ? 'active' : 'locked'}`}>
                  {user.isActive ? tr('Đang hoạt động', 'Active') : tr('Đã bị khóa', 'Locked')}
                </em>
              </div>
            </header>

            <div className="admin-user-info-grid">
              <div>
                <small>{tr('Mã người dùng', 'User ID')}</small>
                <b>#{user.id}</b>
              </div>
              <div>
                <small>{tr('Phân quyền role', 'Role')}</small>
                <select
                  className="user-role-select detail"
                  value={user.roles?.[0] || 'Guest'}
                  disabled={saving}
                  onChange={(event) => changeRole(event.target.value)}
                >
                  <option value="Reader">User</option>
                  <option value="Guest">Guest</option>
                </select>
                <em>{tr('Không thể gán hoặc chỉnh role Admin.', 'The Admin role cannot be assigned or edited here.')}</em>
              </div>
              <div>
                <small>Email</small>
                <b>{user.email}</b>
              </div>
              <div>
                <small>{tr('Xác thực email', 'Email verification')}</small>
                <b>{user.isEmailVerified ? tr('Đã xác thực', 'Verified') : tr('Chưa xác thực', 'Not verified')}</b>
              </div>
              <div>
                <small>{tr('Ngày tạo', 'Created')}</small>
                <b>{formatDate(user.createdAt, locale, tr('Chưa cập nhật', 'Not available'))}</b>
              </div>
              <div>
                <small>{tr('Cập nhật gần nhất', 'Last updated')}</small>
                <b>{formatDate(user.updatedAt, locale, tr('Chưa cập nhật', 'Not available'))}</b>
              </div>
            </div>

            <footer>
              <button
                className={user.isActive ? 'lock-user-button' : 'unlock-user-button'}
                disabled={saving}
                onClick={toggleLock}
              >
                {saving
                  ? tr('Đang xử lý...', 'Processing...')
                  : user.isActive
                    ? tr('Khóa tài khoản', 'Lock account')
                    : tr('Mở khóa tài khoản', 'Unlock account')}
              </button>
            </footer>
          </section>
        ) : (
          !error && <div className="admin-table-card">{tr('Đang tải thông tin người dùng...', 'Loading user details...')}</div>
        )}
      </div>
    </AdminLayout>
  )
}
