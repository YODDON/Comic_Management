import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { adminToast, getCategory, updateCategory } from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminCategoryEditPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { language, tr } = useLanguage()
  const [form, setForm] = useState({ name: '', tag: '' })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    let active = true
    getCategory(id)
      .then((result) => {
        if (!active) return
        const category = result.data
        if (!category?.id) throw new Error(tr('Không tìm thấy thể loại.', 'Genre not found.'))
        setForm({ name: category.name || '', tag: category.tag || '' })
      })
      .catch((error) => {
        if (!active) return
        const text = language === 'en' ? 'Unable to load the genre.' : error.message
        setMessage({ type: 'error', text })
        adminToast('error', tr('Tải thể loại thất bại', 'Could not load genre'), text)
      })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id, language, tr])

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setMessage(null)
    try {
      await updateCategory(id, {
        name: form.name.trim(),
        tag: form.tag.trim(),
      })
      navigate('/admin/categories', {
        replace: true,
        state: { message: tr('Cập nhật thể loại thành công!', 'Genre updated successfully!') },
      })
    } catch (error) {
      const text = language === 'en' ? 'Unable to update the genre.' : error.message
      setMessage({ type: 'error', text })
      adminToast('error', tr('Cập nhật thất bại', 'Update failed'), text)
    } finally {
      setSaving(false)
    }
  }

  return (
    <AdminLayout title={tr('Chỉnh sửa thể loại', 'Edit genre')}>
      <div className="admin-page-heading">
        <div>
          <h2>✎ {tr('Chỉnh sửa thể loại', 'Edit genre')}</h2>
          <p>{tr('Cập nhật tên và tags của thể loại truyện.', 'Update the comic genre name and tags.')}</p>
        </div>
      </div>
      {message && <div className={`admin-alert ${message.type}`}>{message.text}</div>}
      {loading ? (
        <div className="dashboard-loading">{tr('Đang tải thể loại...', 'Loading genre...')}</div>
      ) : (
        <form className="admin-form-page" onSubmit={submit}>
          <label>
            {tr('Tên thể loại', 'Genre name')}
            <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} maxLength="100" autoFocus required />
          </label>
          <label>
            Tags
            <input value={form.tag} onChange={(event) => setForm({ ...form, tag: event.target.value })} maxLength="500" placeholder={tr('Ví dụ: action, hot', 'Example: action, hot')} />
          </label>
          <footer>
            <button type="button" onClick={() => navigate('/admin/categories')}>{tr('Hủy', 'Cancel')}</button>
            <button className="save" disabled={saving}>{saving ? tr('Đang lưu...', 'Saving...') : tr('Lưu thay đổi', 'Save changes')}</button>
          </footer>
        </form>
      )}
    </AdminLayout>
  )
}
