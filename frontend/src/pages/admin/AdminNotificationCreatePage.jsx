import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { createBroadcastNotification } from '../../services/notificationService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminNotificationCreatePage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const [form, setForm] = useState({ title: '', body: '' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  async function submit(event) {
    event.preventDefault(); setSaving(true); setError('')
    try { await createBroadcastNotification(form); navigate('/admin/notifications', { replace: true, state: { message: tr('Đã gửi thông báo đến tất cả người dùng.', 'Notification sent to all users.') } }) }
    catch (requestError) { setError(language === 'en' ? 'Unable to send the notification.' : requestError.message) } finally { setSaving(false) }
  }
  return <AdminLayout title={tr('Thêm thông báo', 'Add notification')}>
    <div className="admin-page-heading"><div><h2>{tr('Thêm thông báo mới', 'Add a new notification')}</h2><p>{tr('Thông báo sẽ được gửi đến tất cả người dùng.', 'The notification will be sent to all users.')}</p></div></div>
    {error && <div className="admin-alert error">{error}</div>}
    <form className="admin-form-page notification-admin-form" onSubmit={submit}><label>{tr('Tiêu đề', 'Title')}<input autoFocus required maxLength="200" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder={tr('Ví dụ: Thông báo bảo trì', 'Example: Maintenance notice')} /></label><label>{tr('Nội dung', 'Content')}<textarea required maxLength="2000" value={form.body} onChange={(e) => setForm({ ...form, body: e.target.value })} placeholder={tr('Hệ thống sẽ bảo trì trong vòng 5 phút nữa.', 'The system will undergo maintenance in five minutes.')} /></label><footer><button type="button" onClick={() => navigate('/admin/notifications')}>{tr('Hủy', 'Cancel')}</button><button className="save" disabled={saving}>{saving ? tr('Đang gửi...', 'Sending...') : tr('Gửi thông báo', 'Send notification')}</button></footer></form>
  </AdminLayout>
}
