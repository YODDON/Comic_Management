import { useState } from 'react'
import { uploadBannerImage } from '../../services/bannerService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function BannerForm({ form, setForm, saving, submit, cancel, submitLabel }) {
  const { language, tr } = useLanguage()
  const [imageMode, setImageMode] = useState('url')
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState('')

  async function chooseFile(event) {
    const file = event.target.files?.[0]
    if (!file) return
    setUploading(true); setUploadError('')
    try {
      const result = await uploadBannerImage(file)
      setForm({ ...form, imageUrl: result.imageUrl, imagePublicId: result.imagePublicId })
    } catch (error) { setUploadError(language === 'en' ? 'Unable to upload the banner image.' : error.message) } finally { setUploading(false) }
  }

  return <form className="admin-form-page banner-admin-form" onSubmit={submit}>
    <div className="banner-form-grid">
      <label>{tr('Tiêu đề banner', 'Banner title')} <small>{tr('Không bắt buộc', 'Optional')}</small>
        <input autoFocus maxLength="200" pattern="[\p{L}\p{N} ]+" title={tr('Chỉ được nhập chữ, số và khoảng trắng.', 'Only letters, numbers and spaces are allowed.')} value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} placeholder={tr('Có thể để trống', 'May be left blank')} />
      </label>
      <label>{tr('Thứ tự hiển thị', 'Display order')}
        <input required type="number" min="0" step="1" value={form.displayOrder} onChange={(event) => setForm({ ...form, displayOrder: event.target.value })} />
      </label>
    </div>
    <fieldset className="banner-image-field">
      <legend>{tr('Ảnh banner', 'Banner image')}</legend>
      <div className="banner-image-tabs"><button type="button" className={imageMode === 'url' ? 'active' : ''} onClick={() => setImageMode('url')}>{tr('Nhập đường dẫn ảnh', 'Enter image URL')}</button><button type="button" className={imageMode === 'file' ? 'active' : ''} onClick={() => setImageMode('file')}>{tr('Tải ảnh từ máy', 'Upload from computer')}</button></div>
      {imageMode === 'url' ? <label>{tr('Đường dẫn ảnh', 'Image URL')}<input required type="url" maxLength="2048" value={form.imageUrl} onChange={(event) => setForm({ ...form, imageUrl: event.target.value, imagePublicId: '' })} placeholder="https://.../banner.jpg" /></label> : <label className="banner-file-label">{tr('Chọn file ảnh', 'Choose image file')}<input type="file" accept="image/*" onChange={chooseFile} /><small>{tr('Định dạng ảnh, dung lượng tối đa 10 MB.', 'Image file, maximum size 10 MB.')}</small></label>}
      {uploading && <p className="banner-upload-state">{tr('Đang tải ảnh lên...', 'Uploading image...')}</p>}
      {uploadError && <p className="banner-upload-error">{uploadError}</p>}
    </fieldset>
    <label>{tr('Đường dẫn khi bấm banner', 'Banner target URL')} <small>{tr('Không bắt buộc', 'Optional')}</small>
      <input type="url" maxLength="2048" value={form.linkUrl} onChange={(event) => setForm({ ...form, linkUrl: event.target.value })} placeholder={tr('Có thể để trống', 'May be left blank')} />
    </label>
    <label className="banner-active-check"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} /> {tr('Hiển thị banner này trên trang chủ', 'Show this banner on the home page')}</label>
    {form.imageUrl && <div className="banner-form-preview"><span>{tr('Xem trước', 'Preview')}</span><img src={form.imageUrl} alt={tr('Xem trước banner', 'Banner preview')} /></div>}
    <footer><button type="button" onClick={cancel}>{tr('Huỷ', 'Cancel')}</button><button className="save" disabled={saving || uploading || !form.imageUrl}>{saving ? tr('Đang lưu...', 'Saving...') : submitLabel}</button></footer>
  </form>
}
