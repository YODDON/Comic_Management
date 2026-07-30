import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import {
  addChapterPagesByUrls,
  adminToast,
  deleteChapterPage,
  deleteChapterPages,
  getChapter,
  getComicBySlug,
  reorderChapterPages,
  updateChapter,
  uploadChapterPages,
} from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

function TrashIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true">
    <path d="M4 7h16M9 7V4h6v3m-9 0 1 14h10l1-14M10 11v6m4-6v6" />
  </svg>
}

export default function AdminChapterEditPage() {
  const { language, tr } = useLanguage()
  const { slug, chapterId } = useParams()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const returnToComic = `/admin/comics/detail/${slug}${searchParams.get('returnEdit') === '1' ? '?edit=1' : ''}`
  const [comic, setComic] = useState(null)
  const [chapter, setChapter] = useState(null)
  const [form, setForm] = useState({ title: '', unitPrice: 0, status: 'Draft' })
  const [files, setFiles] = useState([])
  const [urlText, setUrlText] = useState('')
  const [fileInputKey, setFileInputKey] = useState(0)
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [pages, setPages] = useState([])
  const [reordering, setReordering] = useState(false)
  const [savingOrder, setSavingOrder] = useState(false)
  const [selectedPageIds, setSelectedPageIds] = useState([])
  const [deletingPages, setDeletingPages] = useState(false)
  const translationTexts = useMemo(() => [
    { key: `comic.${comic?.id}.title`, value: comic?.title },
    { key: `chapter.${chapter?.id}.title`, value: chapter?.title },
  ], [chapter, comic])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)
  const comicTitle = translations[`comic.${comic?.id}.title`] || comic?.title
  const chapterTitle = translations[`chapter.${chapter?.id}.title`] || chapter?.title

  const load = useCallback(async () => {
    try {
      const [comicResult, chapterResult] = await Promise.all([
        getComicBySlug(slug),
        getChapter(chapterId, true),
      ])
      if (!chapterResult?.data?.id) throw new Error(tr('Không nhận được chi tiết chương hợp lệ.', 'The API returned invalid chapter details.'))
      const comicData = comicResult?.data || null
      if (comicData?.status === 'Dropped') {
        adminToast('error', tr('Chỉ được xem', 'Read only'), tr('Truyện bị từ chối không thể chỉnh sửa chương.', 'Chapters of a rejected comic cannot be edited.'))
        navigate(`/admin/comics/detail/${slug}`, { replace: true })
        return
      }
      setComic(comicData)
      setChapter(chapterResult.data)
      setForm({
        title: chapterResult.data.title,
        unitPrice: chapterResult.data.unitPrice,
        status: chapterResult.data.status,
      })
      setPages([...(chapterResult.data.chapterPages || [])].sort((a, b) => a.pageNumber - b.pageNumber))
      setSelectedPageIds([])
    } catch (error) {
      adminToast('error', tr('Không thể tải chương', 'Unable to load chapter'), language === 'en' ? 'Unable to load chapter details.' : error.message)
    }
  }, [chapterId, language, navigate, slug, tr])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  async function saveInfo(event) {
    event.preventDefault()
    if (!chapter) return
    setSaving(true)
    try {
      await updateChapter(chapter.id, {
        title: form.title.trim(),
        chapterNumber: chapter.chapterNumber,
        unitPrice: Number(form.unitPrice) || 0,
        status: form.status,
      })
      adminToast('success', tr('Thành công', 'Success'), tr('Đã lưu thông tin chương.', 'Chapter details saved.'))
      await load()
    } catch (error) {
      adminToast('error', tr('Lưu chương thất bại', 'Could not save chapter'), language === 'en' ? 'Unable to save the chapter.' : error.message)
    } finally {
      setSaving(false)
    }
  }

  async function addPages(event) {
    event.preventDefault()
    const urls = urlText.split(/\r?\n/).map((url) => url.trim()).filter(Boolean)
    if (!files.length && !urls.length) {
      adminToast('error', tr('Chưa có ảnh', 'No images selected'), tr('Hãy chọn file ảnh hoặc nhập ít nhất một URL ảnh.', 'Choose image files or enter at least one image URL.'))
      return
    }

    setUploading(true)
    let added = 0
    try {
      if (files.length) {
        await uploadChapterPages(chapterId, files)
        added += files.length
      }
      if (urls.length) {
        const result = await addChapterPagesByUrls(chapterId, urls)
        added += result?.data?.length || urls.length
      }
      adminToast('success', tr('Thêm trang thành công', 'Pages added'), tr(`Đã thêm ${added} trang ảnh vào chương.`, `${added} image page(s) added to the chapter.`))
      setFiles([])
      setUrlText('')
      setFileInputKey((value) => value + 1)
      await load()
    } catch (error) {
      adminToast('error', tr('Thêm trang thất bại', 'Could not add pages'), language === 'en' ? 'Unable to add chapter pages.' : error.message)
      await load()
    } finally {
      setUploading(false)
    }
  }

  async function removePage(page) {
    if (!window.confirm(tr(`Xóa trang ${page.pageNumber} khỏi chương?`, `Delete page ${page.pageNumber} from this chapter?`))) return
    try {
      await deleteChapterPage(chapterId, page.id)
      adminToast('success', tr('Thành công', 'Success'), tr(`Đã xóa trang ${page.pageNumber}.`, `Page ${page.pageNumber} deleted.`))
      await load()
    } catch (error) {
      adminToast('error', tr('Xóa trang thất bại', 'Could not delete page'), language === 'en' ? 'Unable to delete the page.' : error.message)
    }
  }

  function togglePage(pageId) {
    setSelectedPageIds((current) => current.includes(pageId)
      ? current.filter((id) => id !== pageId)
      : [...current, pageId])
  }

  function toggleAllPages() {
    setSelectedPageIds((current) =>
      current.length === pages.length ? [] : pages.map((page) => page.id))
  }

  async function removeSelectedPages() {
    if (!selectedPageIds.length) return
    if (!window.confirm(tr(`Xóa ${selectedPageIds.length} trang ảnh đã chọn khỏi chương?`, `Delete ${selectedPageIds.length} selected image page(s) from this chapter?`))) return
    setDeletingPages(true)
    try {
      await deleteChapterPages(chapterId, selectedPageIds)
      adminToast('success', tr('Thành công', 'Success'), tr(`Đã xóa ${selectedPageIds.length} trang ảnh.`, `${selectedPageIds.length} image page(s) deleted.`))
      await load()
    } catch (error) {
      adminToast('error', tr('Xóa ảnh thất bại', 'Could not delete images'), language === 'en' ? 'Unable to delete the selected images.' : error.message)
    } finally {
      setDeletingPages(false)
    }
  }

  function movePage(index, direction) {
    const target = index + direction
    if (target < 0 || target >= pages.length) return
    setPages((current) => {
      const next = [...current]
      ;[next[index], next[target]] = [next[target], next[index]]
      return next
    })
  }

  async function savePageOrder() {
    setSavingOrder(true)
    try {
      await reorderChapterPages(chapterId, pages.map((page, index) => ({ pageId: page.id, newOrder: index + 1 })))
      adminToast('success', tr('Thành công', 'Success'), tr('Đã lưu thứ tự các trang.', 'Page order saved.'))
      setReordering(false)
      await load()
    } catch (error) {
      adminToast('error', tr('Sắp xếp thất bại', 'Reordering failed'), language === 'en' ? 'Unable to save the page order.' : error.message)
    } finally {
      setSavingOrder(false)
    }
  }

  return <AdminLayout title={`${tr('Sửa chương', 'Edit chapter')}${chapter ? ` - ${chapterTitle}` : ''}`}>
    <div className="chapter-edit-page">
      <div className="admin-page-heading chapter-edit-page-heading">
        <div><h2>✎ {tr('Sửa chương', 'Edit chapter')}</h2><p>{tr('Truyện:', 'Comic:')} <b>{comicTitle || tr('Đang tải...', 'Loading...')}</b></p></div>
        <button className="chapter-back-button" onClick={() => navigate(returnToComic)}>
          <svg viewBox="0 0 16 16" aria-hidden="true"><path d="m10 3-5 5 5 5" /></svg>
          <span>{tr('Quay lại truyện', 'Back to comic')}</span>
        </button>
      </div>

      <form className="chapter-edit-panel" onSubmit={saveInfo}>
        <h3>ⓘ {tr('Thông tin chương', 'Chapter details')}</h3>
        <div className="chapter-edit-info-grid">
          <label>{tr('Tiêu đề chương', 'Chapter title')}<input value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} required /></label>
          <label>{tr('Giá (🌷 Dâu)', 'Price (🌷 Dâu)')}<input type="number" min="0" value={form.unitPrice} onChange={(event) => setForm({ ...form, unitPrice: event.target.value })} /></label>
          <label>{tr('Trạng thái', 'Status')}<select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option value="Draft">{tr('Chờ duyệt', 'Pending')}</option><option value="Published">{tr('Đã duyệt', 'Published')}</option><option value="Hidden">{tr('Ẩn', 'Hidden')}</option></select></label>
        </div>
        <button className="chapter-primary-button" disabled={!chapter || saving}>▣ {saving ? tr('Đang lưu...', 'Saving...') : tr('Lưu thay đổi', 'Save changes')}</button>
      </form>

      <form className="chapter-edit-panel chapter-upload-panel" onSubmit={addPages}>
        <h3>▧ {tr('Thêm trang ảnh', 'Add image pages')}</h3>
        <label>{tr('Tải ảnh trang lên (chọn nhiều file)', 'Upload page images (multiple files)')}
          <input key={fileInputKey} type="file" accept="image/*" multiple onChange={(event) => setFiles(Array.from(event.target.files || []))} />
          <small>{tr(`Ảnh sẽ được upload lên Cloudinary và tự động thêm vào cuối chương. Đã chọn ${files.length} ảnh.`, `Images will be uploaded to Cloudinary and appended to the chapter. ${files.length} image(s) selected.`)}</small>
        </label>
        <label>{tr('Hoặc nhập URL ảnh (mỗi URL một dòng)', 'Or enter image URLs (one per line)')}
          <textarea rows="4" value={urlText} onChange={(event) => setUrlText(event.target.value)} placeholder={'https://image1.jpg\nhttps://image2.jpg'} />
        </label>
        <button className="chapter-primary-button" disabled={uploading}>＋ {uploading ? tr('Đang thêm trang...', 'Adding pages...') : tr('Thêm trang ảnh', 'Add image pages')}</button>
      </form>

      <section className="chapter-edit-panel chapter-pages-panel">
        <div className="chapter-pages-heading">
          <h3>☷ {tr('Danh sách trang', 'Page list')} ({pages.length})</h3>
          <div>
            <button
              type="button"
              className="chapter-delete-selected"
              disabled={!selectedPageIds.length || deletingPages || reordering}
              onClick={removeSelectedPages}
            >
              <TrashIcon /> {deletingPages ? tr('Đang xóa...', 'Deleting...') : tr(`Xóa đã chọn (${selectedPageIds.length})`, `Delete selected (${selectedPageIds.length})`)}
            </button>
            {reordering && <button className="chapter-cancel-order" onClick={() => { setReordering(false); load() }}>{tr('Hủy', 'Cancel')}</button>}
            <button className="chapter-order-button" disabled={!pages.length || savingOrder} onClick={() => reordering ? savePageOrder() : setReordering(true)}>{reordering ? `▣ ${savingOrder ? tr('Đang lưu...', 'Saving...') : tr('Lưu thứ tự', 'Save order')}` : `↕ ${tr('Sắp xếp lại', 'Reorder')}`}</button>
          </div>
        </div>
        <div className="admin-table-wrap"><table className="chapter-pages-table">
          <colgroup><col className="page-select-column" /><col className="page-order-column" /><col className="page-image-column" /><col className="page-url-column" /><col className="page-action-column" /></colgroup>
          <thead><tr><th><input type="checkbox" aria-label={tr('Chọn tất cả trang ảnh', 'Select all image pages')} checked={pages.length > 0 && selectedPageIds.length === pages.length} disabled={!pages.length || reordering || deletingPages} onChange={toggleAllPages} /></th><th>{tr('THỨ TỰ', 'ORDER')}</th><th>{tr('ẢNH', 'IMAGE')}</th><th>URL</th><th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th></tr></thead>
          <tbody>
            {!pages.length && <tr><td className="empty-cell" colSpan="5">{tr('Chương chưa có trang ảnh.', 'This chapter has no image pages.')}</td></tr>}
            {pages.map((page, index) => <tr key={page.id}>
              <td><input type="checkbox" aria-label={tr(`Chọn trang ${index + 1}`, `Select page ${index + 1}`)} checked={selectedPageIds.includes(page.id)} disabled={reordering || deletingPages} onChange={() => togglePage(page.id)} /></td>
              <td><b>{index + 1}</b></td>
              <td><img className="chapter-page-thumbnail" src={page.imageUrl} alt={`Trang ${index + 1}`} /></td>
              <td><a className="chapter-page-url" href={page.imageUrl} target="_blank" rel="noreferrer">{page.imageUrl}</a></td>
              <td><div className="chapter-page-actions">
                {reordering && <><button disabled={index === 0} onClick={() => movePage(index, -1)}>↑</button><button disabled={index === pages.length - 1} onClick={() => movePage(index, 1)}>↓</button></>}
                {!reordering && <button className="delete" aria-label={tr(`Xóa trang ${index + 1}`, `Delete page ${index + 1}`)} title={tr('Xóa trang', 'Delete page')} onClick={() => removePage(page)}><TrashIcon /></button>}
              </div></td>
            </tr>)}
          </tbody>
        </table></div>
      </section>
      {translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </div>
  </AdminLayout>
}
