import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import BannerForm from '../../components/admin/BannerForm'
import { getAdminBanner, updateBanner } from '../../services/bannerService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminBannerEditPage() {
  const { language, tr } = useLanguage()
  const { id } = useParams()
  const navigate = useNavigate()
  const [form, setForm] = useState(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    getAdminBanner(id).then((item) => setForm({ title: item.title || '', imageUrl: item.imageUrl, imagePublicId: '', linkUrl: item.linkUrl || '', isActive: item.isActive, displayOrder: item.displayOrder })).catch((requestError) => setError(language === 'en' ? 'Unable to load the banner.' : requestError.message))
  }, [id, language])

  async function submit(event) {
    event.preventDefault(); setSaving(true); setError('')
    try {
      await updateBanner(id, { ...form, title: form.title.trim() || null, displayOrder: Number(form.displayOrder), linkUrl: form.linkUrl.trim() || null })
      navigate('/admin/banners', { replace: true, state: { message: tr('Đã cập nhật banner.', 'Banner updated.') } })
    } catch (requestError) { setError(language === 'en' ? 'Unable to update the banner.' : requestError.message) } finally { setSaving(false) }
  }

  return <AdminLayout title={tr('Sửa Banner', 'Edit banner')}><div className="banner-editor-page"><div className="admin-page-heading banner-editor-heading"><div><h2>{tr('Sửa banner', 'Edit banner')}</h2><p>{tr('Cập nhật ảnh, liên kết, trạng thái và thứ tự hiển thị trên trang chủ.', 'Update the image, link, status and home page display order.')}</p></div></div>{error && <div className="admin-alert error">{error}</div>}{form ? <BannerForm form={form} setForm={setForm} saving={saving} submit={submit} cancel={() => navigate('/admin/banners')} submitLabel={tr('Lưu thay đổi', 'Save changes')} /> : !error && <div className="admin-table-card">{tr('Đang tải banner...', 'Loading banner...')}</div>}</div></AdminLayout>
}
