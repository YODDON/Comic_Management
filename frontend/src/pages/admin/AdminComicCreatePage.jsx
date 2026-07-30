import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { adminToast, createComic, getCategories, updateComicStatus } from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

function createSlug(value) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').replace(/Đ/g, 'D').toLowerCase().trim().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}

export default function AdminComicCreatePage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const [categories, setCategories] = useState([])
  const [cover, setCover] = useState(null)
  const [slugEdited, setSlugEdited] = useState(false)
  const [form, setForm] = useState({ title: '', slug: '', author: '', coverUrl: '', description: '', unitPrice: 0, salaryType: '0', categoryIds: [] })
  const [saving, setSaving] = useState(false)
  const translationTexts = useMemo(() => categories.map((category) => ({ key: `category.${category.id}.name`, value: category.name })), [categories])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)
  useEffect(() => { getCategories({ pageSize: 100 }).then((result) => setCategories(result.data?.items || [])).catch(() => {}) }, [])

  function changeTitle(title) {
    setForm((current) => ({ ...current, title, slug: slugEdited ? current.slug : createSlug(title) }))
  }

  function toggleCategory(categoryId) {
    setForm((current) => ({
      ...current,
      categoryIds: current.categoryIds.includes(categoryId)
        ? current.categoryIds.filter((id) => id !== categoryId)
        : [...current.categoryIds, categoryId],
    }))
  }

  async function submit(event) {
    event.preventDefault()
    if (!cover && !form.coverUrl.trim()) return adminToast('error', tr('Thiếu ảnh bìa', 'Cover image required'), tr('Vui lòng nhập URL ảnh bìa hoặc chọn file ảnh.', 'Enter a cover image URL or choose an image file.'))
    const body = new FormData()
    body.append('Title', form.title)
    body.append('Slug', form.slug)
    body.append('Author', form.author)
    body.append('Description', form.description)
    body.append('UnitPrice', form.unitPrice)
    body.append('SalaryType', form.salaryType)
    form.categoryIds.forEach((id) => body.append('CategoryIds', id))
    if (cover) body.append('CoverImage', cover)
    else body.append('CoverUrl', form.coverUrl.trim())
    setSaving(true)
    try {
      const result = await createComic(body)
      await updateComicStatus(result.data.id, 'Completed')
      navigate(`/admin/comics/detail/${result.data.slug}`)
      window.setTimeout(() => adminToast('success', tr('Thành công', 'Success'), tr('Đã tạo truyện và chuyển sang trạng thái chờ duyệt.', 'Comic created and moved to pending review.')), 100)
    } catch (error) { adminToast('error', tr('Tạo truyện thất bại', 'Could not create comic'), language === 'en' ? 'Unable to create the comic.' : error.message) }
    finally { setSaving(false) }
  }

  return <AdminLayout title={tr('Tạo truyện', 'Create comic')}>
    <div className="admin-page-heading comic-create-heading"><div><h2>＋ {tr('Tạo truyện mới', 'Create a new comic')}</h2><p>{tr('Tạo mới một truyện và gửi lên hệ thống.', 'Create a comic and submit it to the system.')}</p></div><button className="comic-back-button" onClick={() => navigate('/admin/comics/pending')}>← {tr('Quay lại', 'Back')}</button></div>
    <form className="admin-form-page comic-create-form reference-create-form" onSubmit={submit}>
      <div className="comic-create-grid">
        <label>{tr('Tiêu đề', 'Title')}<input value={form.title} onChange={(e) => changeTitle(e.target.value)} placeholder={tr('Nhập tiêu đề truyện', 'Enter comic title')} required /></label>
        <label>{tr('Đường dẫn (Slug)', 'Slug')} <small>{tr('(tự động tạo từ tiêu đề)', '(generated automatically from the title)')}</small><div className="slug-input-row"><input value={form.slug} onChange={(e) => { setSlugEdited(true); setForm({ ...form, slug: createSlug(e.target.value) }) }} required /><button type="button" title={tr('Tạo lại slug', 'Regenerate slug')} onClick={() => { setSlugEdited(false); setForm({ ...form, slug: createSlug(form.title) }) }}>↻</button></div></label>
        <label>{tr('Tác giả', 'Author')}<input value={form.author} onChange={(e) => setForm({ ...form, author: e.target.value })} placeholder={tr('Tên tác giả', 'Author name')} /></label>
        <label>{tr('Ảnh bìa (URL)', 'Cover image (URL)')}<input type="url" value={form.coverUrl} onChange={(e) => setForm({ ...form, coverUrl: e.target.value })} placeholder="https://..." disabled={Boolean(cover)} /></label>
        <label className="cover-upload">{tr('Hoặc tải ảnh bìa lên', 'Or upload a cover image')}<input type="file" accept="image/png,image/jpeg,image/webp,image/gif" onChange={(e) => setCover(e.target.files?.[0] || null)} /><small>{tr('Hỗ trợ JPG, PNG, WebP, GIF (tối đa 5MB). File upload được ưu tiên nếu có.', 'Supports JPG, PNG, WebP and GIF (up to 5 MB). Uploaded files take priority.')}</small></label>
        <label>{tr('Mức nhuận bút cơ bản (🌷 Dâu)', 'Base royalty (🌷 Dâu)')}<input type="number" min="0" value={form.unitPrice} disabled={form.salaryType === '2'} onChange={(e) => setForm({ ...form, unitPrice: Number(e.target.value) })} /><small>{form.salaryType === '2' ? tr('Truyện miễn phí không áp dụng nhuận bút.', 'Free comics do not receive royalties.') : tr('Nhập mức Dâu cơ bản dùng cho hình thức phát hành đã chọn.', 'Enter the base Dâu amount for the selected publishing model.')}</small></label>
        <label>{tr('Hình thức nhuận bút', 'Royalty model')}<select value={form.salaryType} onChange={(e) => setForm({ ...form, salaryType: e.target.value, unitPrice: e.target.value === '2' ? 0 : form.unitPrice })}><option value="0">{tr('Theo lượt đọc', 'Per read')}</option><option value="1">{tr('Độc quyền', 'Exclusive')}</option><option value="2">{tr('Miễn phí (không nhuận bút)', 'Free (no royalty)')}</option></select><small>{tr('Chọn cách tính nhuận bút phù hợp với hình thức phát hành truyện.', 'Choose the royalty model for this comic.')}</small></label>
        <fieldset className="comic-category-field"><legend>{tr('Thể loại', 'Genres')} <small>{tr('(có thể chọn nhiều)', '(multiple selections allowed)')}</small></legend><div className="category-checks">{categories.map((category) => <label key={category.id}><input type="checkbox" checked={form.categoryIds.includes(category.id)} onChange={() => toggleCategory(category.id)} /><span>{translations[`category.${category.id}.name`] || category.name}</span></label>)}</div>{!categories.length && <small>{tr('Chưa có thể loại để chọn.', 'No genres are available.')}</small>}</fieldset>
        <label className="comic-description-field">{tr('Mô tả', 'Description')}<textarea rows="7" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder={tr('Nhập mô tả truyện...', 'Enter comic description...')} /></label>
      </div>
      <footer className="comic-create-footer"><button className="save" disabled={saving}>{saving ? tr('Đang tạo...', 'Creating...') : `＋ ${tr('Tạo truyện', 'Create comic')}`}</button><button type="button" className="cancel-link" onClick={() => navigate('/admin/comics/pending')}>{tr('Hủy', 'Cancel')}</button></footer>
      {translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </form>
  </AdminLayout>
}
