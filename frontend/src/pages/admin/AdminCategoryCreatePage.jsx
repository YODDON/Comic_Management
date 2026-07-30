import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { adminToast, createCategory } from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminCategoryCreatePage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const [form, setForm] = useState({ name: '', tag: '' })
  const [message, setMessage] = useState(null)
  const [saving, setSaving] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setMessage(null)
    try {
      await createCategory(form)
      navigate('/admin/categories', { replace: true })
    } catch (error) { const errorMessage = language === 'en' ? 'Unable to create the genre.' : error.message; setMessage({ type: 'error', text: errorMessage }); adminToast('error', tr('Thêm thể loại thất bại', 'Could not add genre'), errorMessage) }
    finally { setSaving(false) }
  }

  return <AdminLayout title={tr('Thêm thể loại', 'Add genre')}>
    <div className="admin-page-heading"><div><h2>{tr('Thêm thể loại mới', 'Add a new genre')}</h2><p>{tr('Nhập thông tin để tạo một thể loại truyện.', 'Enter the information for the new comic genre.')}</p></div></div>
    {message && <div className="admin-alert error">{message.text}</div>}
    <form className="admin-form-page" onSubmit={submit}>
      <label>{tr('Tên thể loại', 'Genre name')}<input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} maxLength="100" autoFocus required /></label>
      <label>Tags<input value={form.tag} onChange={(event) => setForm({ ...form, tag: event.target.value })} maxLength="500" placeholder={tr('Ví dụ: action, hot', 'Example: action, hot')} /></label>
      <footer><button type="button" onClick={() => navigate('/admin/categories')}>{tr('Hủy', 'Cancel')}</button><button className="save" disabled={saving}>{saving ? tr('Đang lưu...', 'Saving...') : tr('Thêm thể loại', 'Add genre')}</button></footer>
    </form>
  </AdminLayout>
}
