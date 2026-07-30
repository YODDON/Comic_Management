import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { getAdminNotifications, updateAdminNotification } from '../../services/notificationService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminNotificationEditPage() {
  const { language, tr } = useLanguage()
  const { id } = useParams()
  const navigate = useNavigate()
  const [form, setForm] = useState(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  useEffect(() => {
    getAdminNotifications().then((result) => {
      const item = (result.data || []).find((notification) => notification.id === id)
      if (!item) throw new Error(tr('Không tìm thấy thông báo.', 'Notification not found.'))
      setForm({ title: item.title, body: item.body, isActive: item.isActive })
    }).catch((requestError) => setError(language === 'en' ? 'Unable to load the notification.' : requestError.message))
  }, [id, language, tr])
  async function submit(event) {
    event.preventDefault(); setSaving(true); setError('')
    try { await updateAdminNotification(id, form); navigate('/admin/notifications', { replace: true, state: { message: tr('Đã cập nhật thông báo.', 'Notification updated.') } }) }
    catch (requestError) { setError(language === 'en' ? 'Unable to update the notification.' : requestError.message) } finally { setSaving(false) }
  }
  return <AdminLayout title={tr('Sửa thông báo', 'Edit notification')}>
    <div className="admin-page-heading"><div><h2>{tr('Sửa thông báo', 'Edit notification')}</h2><p>{tr('Cập nhật nội dung và trạng thái hiển thị.', 'Update the notification content and visibility.')}</p></div></div>
    {error && <div className="admin-alert error">{error}</div>}
    {!form && !error && <div className="admin-table-card empty-cell">{tr('Đang tải thông báo...', 'Loading notification...')}</div>}
    {form && <form className="admin-form-page notification-admin-form" onSubmit={submit}><label>{tr('Tiêu đề', 'Title')}<input autoFocus required maxLength="200" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label><label>{tr('Nội dung', 'Content')}<textarea required maxLength="2000" value={form.body} onChange={(e) => setForm({ ...form, body: e.target.value })} /></label><label className="notification-active-check"><input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} /> {tr('Đang hiển thị cho người dùng', 'Visible to users')}</label><footer><button type="button" onClick={() => navigate('/admin/notifications')}>{tr('Hủy', 'Cancel')}</button><button className="save" disabled={saving}>{saving ? tr('Đang lưu...', 'Saving...') : tr('Lưu thay đổi', 'Save changes')}</button></footer></form>}
  </AdminLayout>
}
