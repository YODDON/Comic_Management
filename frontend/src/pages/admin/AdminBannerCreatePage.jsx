import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import BannerForm from '../../components/admin/BannerForm'
import { createBanner } from '../../services/bannerService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminBannerCreatePage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const [form, setForm] = useState({ title: '', imageUrl: '', linkUrl: '', isActive: true, displayOrder: 0 })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault(); setSaving(true); setError('')
    try {
      await createBanner({ ...form, title: form.title.trim() || null, displayOrder: Number(form.displayOrder), linkUrl: form.linkUrl.trim() || null })
      navigate('/admin/banners', { replace: true, state: { message: tr('Đã thêm banner mới. Banner đang hoạt động sẽ xuất hiện trên trang chủ.', 'New banner added. Active banners will appear on the home page.') } })
    } catch (requestError) { setError(language === 'en' ? 'Unable to create the banner.' : requestError.message) } finally { setSaving(false) }
  }

  return <AdminLayout title={tr('Thêm Banner', 'Add banner')}><div className="banner-editor-page"><div className="admin-page-heading banner-editor-heading"><div><h2>{tr('Thêm banner mới', 'Add a new banner')}</h2><p>{tr('Banner hoạt động sẽ được hiển thị trong slider trang chủ theo thứ tự đã nhập.', 'Active banners appear in the home page slider in the specified order.')}</p></div></div>{error && <div className="admin-alert error">{error}</div>}<BannerForm form={form} setForm={setForm} saving={saving} submit={submit} cancel={() => navigate('/admin/banners')} submitLabel={tr('Lưu banner', 'Save banner')} /></div></AdminLayout>
}
