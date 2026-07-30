import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import {
  adminToast,
  createChapter,
  deleteChapter,
  getCategories,
  getChapters,
  getComicBySlug,
  toggleOutstanding,
  updateComic,
  uploadComicCover,
} from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

const emptyChapter = { title: '', unitPrice: 0, status: 'Draft' }
const statusLabels = { Draft: ['Chờ duyệt', 'Pending'], Published: ['Đã duyệt', 'Published'], Hidden: ['Ẩn', 'Hidden'] }
const comicStatusValues = { Ongoing: 0, Completed: 1, Dropped: 2 }

function createSlug(value) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
}

export default function AdminComicDetailPage() {
  const { language, tr } = useLanguage()
  const { slug } = useParams()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [comic, setComic] = useState(null)
  const [chapters, setChapters] = useState([])
  const [categories, setCategories] = useState([])
  const [chapter, setChapter] = useState(emptyChapter)
  const [editForm, setEditForm] = useState(null)
  const [editingInfo, setEditingInfo] = useState(searchParams.get('edit') === '1')
  const [search, setSearch] = useState('')
  const [saving, setSaving] = useState(false)
  const [savingInfo, setSavingInfo] = useState(false)
  const [imageMode, setImageMode] = useState('url')
  const [uploadingCover, setUploadingCover] = useState(false)
  const [coverError, setCoverError] = useState('')
  const translationTexts = useMemo(() => [
    { key: `comic.${comic?.id}.title`, value: comic?.title },
    { key: `comic.${comic?.id}.author`, value: comic?.authorName },
    { key: `comic.${comic?.id}.description`, value: comic?.description },
    ...(comic?.categories || []).map((item) => ({ key: `comic.category.${item.id}`, value: item.name })),
    ...categories.map((item) => ({ key: `category.${item.id}`, value: item.name })),
    ...chapters.map((item) => ({ key: `chapter.${item.id}.title`, value: item.title })),
  ], [categories, chapters, comic])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)
  const displayComicTitle = translations[`comic.${comic?.id}.title`] || comic?.title
  const isRejected = comic?.status === 'Dropped'

  const load = useCallback(async () => {
    try {
      const result = await getComicBySlug(slug)
      const comicData = result?.data
      if (!comicData?.id) {
        throw new Error(tr('Không nhận được thông tin truyện hợp lệ từ ComicAPI.', 'ComicAPI returned invalid comic data.'))
      }

      const [chapterResult, categoryResult] = await Promise.all([
        getChapters(comicData.id),
        getCategories({ page: 1, pageSize: 100 }),
      ])
      if (!chapterResult?.data) {
        throw new Error(tr('Không nhận được danh sách chương hợp lệ từ ChapterAPI.', 'ChapterAPI returned an invalid chapter list.'))
      }

      setComic(comicData)
      if (comicData.status === 'Dropped') setEditingInfo(false)
      setChapters(chapterResult.data.items || [])
      setCategories(categoryResult.data?.items || [])
      setEditForm({
        title: comicData.title || '',
        description: comicData.description || '',
        author: comicData.authorName || '',
        thumbnailUrl: comicData.coverUrl || '',
        status: comicData.status || 'Ongoing',
        categoryIds: (comicData.categories || []).map((item) => item.id),
      })
    } catch (error) {
      adminToast('error', tr('Không thể tải truyện', 'Unable to load comic'), language === 'en' ? 'Unable to load the comic details.' : error.message)
    }
  }, [language, slug, tr])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  const nextChapterNumber = useMemo(
    () => Math.max(0, ...chapters.map((item) => Number(item.chapterNumber) || 0)) + 1,
    [chapters],
  )

  const filteredChapters = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase('vi')
    if (!keyword) return chapters
    return chapters.filter((item) =>
      `${item.chapterNumber} ${item.title} ${item.slug}`
        .toLocaleLowerCase('vi')
        .includes(keyword))
  }, [chapters, search])

  async function saveComicInfo(event) {
    event.preventDefault()
    if (!comic || !editForm) return
    if (isRejected) {
      adminToast('error', tr('Chỉ được xem', 'Read only'), tr('Truyện bị từ chối không thể chỉnh sửa.', 'Rejected comics cannot be edited.'))
      return
    }
    setSavingInfo(true)
    try {
      const result = await updateComic(comic.id, {
        title: editForm.title.trim(),
        description: editForm.description.trim(),
        author: editForm.author.trim(),
        thumbnailUrl: editForm.thumbnailUrl.trim(),
        status: comicStatusValues[editForm.status],
        categoryIds: editForm.categoryIds,
      })
      adminToast('success', tr('Thành công', 'Success'), tr('Đã cập nhật thông tin truyện.', 'Comic details updated.'))
      setEditingInfo(false)
      const updatedSlug = result?.data?.slug || slug
      navigate(`/admin/comics/detail/${updatedSlug}`, { replace: true })
      if (updatedSlug === slug) await load()
    } catch (error) {
      adminToast('error', tr('Cập nhật thất bại', 'Update failed'), language === 'en' ? 'Unable to update the comic.' : error.message)
    } finally {
      setSavingInfo(false)
    }
  }

  function toggleCategory(categoryId) {
    setEditForm((current) => ({
      ...current,
      categoryIds: current.categoryIds.includes(categoryId)
        ? current.categoryIds.filter((id) => id !== categoryId)
        : [...current.categoryIds, categoryId],
    }))
  }

  async function chooseCoverFile(event) {
    const file = event.target.files?.[0]
    if (!file) return
    setUploadingCover(true)
    setCoverError('')
    try {
      const result = await uploadComicCover(file)
      const thumbnailUrl = result?.data?.thumbnailUrl
      if (!thumbnailUrl) throw new Error(tr('API không trả về đường dẫn ảnh bìa.', 'The API did not return a cover image URL.'))
      setEditForm((current) => ({ ...current, thumbnailUrl }))
    } catch (error) {
      setCoverError(language === 'en' ? 'Unable to upload the cover image.' : error.message)
    } finally {
      setUploadingCover(false)
    }
  }

  async function addChapter(event) {
    event.preventDefault()
    if (!comic) return
    if (isRejected) {
      adminToast('error', tr('Chỉ được xem', 'Read only'), tr('Không thể thêm chương vào truyện bị từ chối.', 'Chapters cannot be added to a rejected comic.'))
      return
    }
    setSaving(true)
    try {
      await createChapter({
        comicId: comic.id,
        chapterNumber: nextChapterNumber,
        title: chapter.title.trim(),
        unitPrice: Number(chapter.unitPrice) || 0,
        status: chapter.status,
      })
      adminToast('success', tr('Thành công', 'Success'), tr(`Đã thêm chương ${nextChapterNumber}.`, `Chapter ${nextChapterNumber} added.`))
      setChapter(emptyChapter)
      await load()
    } catch (error) {
      adminToast('error', tr('Thêm chương thất bại', 'Could not add chapter'), language === 'en' ? 'Unable to add the chapter.' : error.message)
    } finally {
      setSaving(false)
    }
  }

  async function remove(item) {
    if (isRejected) {
      adminToast('error', tr('Chỉ được xem', 'Read only'), tr('Không thể xóa chương của truyện bị từ chối.', 'Chapters cannot be deleted from a rejected comic.'))
      return
    }
    if (!window.confirm(tr(`Xóa chương “${item.title}”? Các trang ảnh của chương cũng sẽ bị xóa.`, `Delete chapter “${translations[`chapter.${item.id}.title`] || item.title}”? Its image pages will also be deleted.`))) return
    try {
      await deleteChapter(item.id)
      adminToast('success', tr('Thành công', 'Success'), tr('Đã xóa chương.', 'Chapter deleted.'))
      await load()
    } catch (error) {
      adminToast('error', tr('Xóa chương thất bại', 'Could not delete chapter'), language === 'en' ? 'Unable to delete the chapter.' : error.message)
    }
  }

  async function feature() {
    try {
      const result = await toggleOutstanding(comic.id)
      const enabled = Boolean(result?.data?.isOutstanding)
      setComic((current) => ({ ...current, isOutstanding: enabled }))
      adminToast(
        'success',
        tr('Thành công', 'Success'),
        enabled ? tr('Đã bật truyện nổi bật.', 'Comic marked as featured.') : tr('Đã tắt truyện nổi bật.', 'Comic removed from featured.'),
      )
    } catch (error) {
      adminToast('error', tr('Không thể đánh dấu', 'Unable to update featured status'), language === 'en' ? 'Unable to update the featured status.' : error.message)
    }
  }

  return (
    <AdminLayout title={`${tr('Chi tiết truyện', 'Comic details')}${comic ? ` - ${displayComicTitle}` : ''}`}>
      <div className="admin-page-heading">
        <div><h2>ⓘ {displayComicTitle || tr('Đang tải...', 'Loading...')}</h2><p>Slug: {comic?.slug}</p></div>
        <div className="heading-actions">
          <button onClick={() => navigate(isRejected ? '/admin/comics/rejected' : '/admin/comics/approved')}>☷ {tr('Danh sách', 'List')}</button>
          {!isRejected && <button className="edit-comic-button" onClick={() => setEditingInfo((value) => !value)}>
            ✎ {editingInfo ? tr('Đóng chỉnh sửa', 'Close editor') : tr('Sửa thông tin', 'Edit details')}
          </button>}
          {!isRejected && <button
            className={`star-button ${comic?.isOutstanding ? 'active' : ''}`}
            aria-pressed={Boolean(comic?.isOutstanding)}
            onClick={feature}
          >
            ★ {comic?.isOutstanding ? tr('Đang nổi bật', 'Featured') : tr('Đánh dấu nổi bật', 'Mark as featured')}
          </button>}
        </div>
      </div>

      {comic && (
        <section className="comic-detail-card">
          <div className="comic-detail-grid">
            <p><b>{tr('Tác giả:', 'Author:')}</b> {translations[`comic.${comic.id}.author`] || comic.authorName || '-'}</p>
            <p><b>{tr('Trạng thái:', 'Status:')}</b> <span className={`status-badge ${comic.status?.toLowerCase()}`}>{comic.status}</span></p>
            <p><b>{tr('Tổng chương:', 'Total chapters:')}</b> {chapters.length}</p>
            <p><b>{tr('Lượt xem:', 'Views:')}</b> {comic.viewCount}</p>
            <p><b>{tr('Thể loại:', 'Genres:')}</b> {comic.categories?.map((item) => translations[`comic.category.${item.id}`] || item.name).join(', ') || '-'}</p>
            <p><b>ID:</b> {comic.id}</p>
          </div>
          <p className="comic-description"><b>{tr('Mô tả:', 'Description:')}</b><br />{translations[`comic.${comic.id}.description`] || comic.description || '-'}</p>
        </section>
      )}

      {isRejected && <div className="admin-alert warn">{tr('Truyện đã bị từ chối nên chỉ được xem chi tiết, không thể chỉnh sửa thông tin hoặc chương.', 'This comic was rejected and is read-only. Its details and chapters cannot be edited.')}</div>}

      {!isRejected && editingInfo && editForm && (
        <form className="comic-info-edit-card" onSubmit={saveComicInfo}>
          <div className="comic-info-edit-heading">
            <div><h3>✎ {tr('Chỉnh sửa thông tin truyện', 'Edit comic details')}</h3><p>{tr('Cập nhật tác giả, trạng thái, thể loại và nội dung truyện.', 'Update the author, status, genres and comic content.')}</p></div>
          </div>
          <div className="comic-info-edit-grid">
            <label>
              {tr('Tên truyện', 'Comic title')}
              <input
                required
                maxLength="200"
                value={editForm.title}
                onChange={(event) => setEditForm({ ...editForm, title: event.target.value })}
              />
            </label>
            <label>
              {tr('Slug mới (tự động từ tên truyện)', 'New slug (generated from the title)')}
              <input value={createSlug(editForm.title)} readOnly />
              <small>{tr('Nếu slug đã tồn tại, hệ thống sẽ tự thêm hậu tố số.', 'If the slug exists, a numeric suffix will be added automatically.')}</small>
            </label>
            <label>
              {tr('Tác giả', 'Author')}
              <input
                type="text"
                maxLength="255"
                value={editForm.author}
                onChange={(event) => setEditForm({ ...editForm, author: event.target.value })}
                placeholder={tr('Nhập tên tác giả', 'Enter author name')}
              />
            </label>
            <label>
              {tr('Trạng thái', 'Status')}
              <select
                value={editForm.status}
                onChange={(event) => setEditForm({ ...editForm, status: event.target.value })}
              >
                <option value="Ongoing">{tr('Đã duyệt', 'Approved')}</option>
                <option value="Completed">{tr('Chờ duyệt', 'Pending')}</option>
                <option value="Dropped">{tr('Từ chối', 'Rejected')}</option>
              </select>
            </label>
            <fieldset className="comic-thumbnail-field">
              <legend>{tr('Ảnh bìa truyện', 'Comic cover')}</legend>
              <div className="comic-image-tabs">
                <button
                  type="button"
                  className={imageMode === 'url' ? 'active' : ''}
                  onClick={() => setImageMode('url')}
                >
                  {tr('Nhập đường dẫn ảnh', 'Enter image URL')}
                </button>
                <button
                  type="button"
                  className={imageMode === 'file' ? 'active' : ''}
                  onClick={() => setImageMode('file')}
                >
                  {tr('Tải ảnh từ máy', 'Upload from computer')}
                </button>
              </div>
              {imageMode === 'url' ? (
                <label>
                  Thumbnail URL
                  <input
                    type="url"
                    required
                    value={editForm.thumbnailUrl}
                    onChange={(event) => setEditForm({ ...editForm, thumbnailUrl: event.target.value })}
                    placeholder="https://.../cover.jpg"
                  />
                </label>
              ) : (
                <label className="comic-cover-file-label">
                  {tr('Chọn file ảnh', 'Choose image file')}
                  <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/gif"
                    onChange={chooseCoverFile}
                  />
                  <small>{tr('Hỗ trợ JPG, JPEG, PNG, WebP, GIF; tối đa 5 MB.', 'Supports JPG, JPEG, PNG, WebP and GIF; maximum 5 MB.')}</small>
                </label>
              )}
              {uploadingCover && <p className="comic-cover-upload-state">{tr('Đang tải ảnh lên...', 'Uploading image...')}</p>}
              {coverError && <p className="comic-cover-upload-error">{coverError}</p>}
              {editForm.thumbnailUrl && (
                <div className="comic-cover-preview">
                  <span>{tr('Xem trước', 'Preview')}</span>
                  <img src={editForm.thumbnailUrl} alt={tr('Xem trước ảnh bìa', 'Cover preview')} />
                </div>
              )}
            </fieldset>
            <label className="comic-description-field">
              {tr('Mô tả', 'Description')}
              <textarea
                rows="5"
                value={editForm.description}
                onChange={(event) => setEditForm({ ...editForm, description: event.target.value })}
              />
            </label>
          </div>
          <fieldset className="comic-category-editor">
            <legend>{tr('Thể loại', 'Genres')}</legend>
            <div>
              {categories.map((category) => (
                <label key={category.id}>
                  <input
                    type="checkbox"
                    checked={editForm.categoryIds.includes(category.id)}
                    onChange={() => toggleCategory(category.id)}
                  />
                  {translations[`category.${category.id}`] || category.name}
                </label>
              ))}
            </div>
          </fieldset>
          <footer>
            <button type="button" onClick={() => setEditingInfo(false)}>{tr('Hủy', 'Cancel')}</button>
            <button className="save" disabled={savingInfo || uploadingCover || !editForm.thumbnailUrl}>
              {savingInfo ? tr('Đang lưu...', 'Saving...') : tr('Lưu thay đổi', 'Save changes')}
            </button>
          </footer>
        </form>
      )}

      {!isRejected && <form className="chapter-create-card" onSubmit={addChapter}>
        <h3>＋ {tr('Thêm chương mới', 'Add a new chapter')}</h3>
        <div className="chapter-form-grid chapter-main-fields">
          <label>{tr('Tiêu đề chương', 'Chapter title')}<input value={chapter.title} onChange={(event) => setChapter({ ...chapter, title: event.target.value })} placeholder={tr(`Chương ${nextChapterNumber}: ...`, `Chapter ${nextChapterNumber}: ...`)} required /></label>
          <label>{tr('Giá (🌷 Dâu)', 'Price (🌷 Dâu)')}<input type="number" min="0" value={chapter.unitPrice} onChange={(event) => setChapter({ ...chapter, unitPrice: event.target.value })} /></label>
          <label>{tr('Trạng thái', 'Status')}<select value={chapter.status} onChange={(event) => setChapter({ ...chapter, status: event.target.value })}><option value="Draft">{tr('Chờ duyệt', 'Pending')}</option><option value="Published">{tr('Đã duyệt', 'Published')}</option><option value="Hidden">{tr('Ẩn', 'Hidden')}</option></select></label>
        </div>
        <div className="chapter-submit-row"><button className="chapter-add-button" disabled={saving}>{saving ? tr('Đang thêm...', 'Adding...') : `＋ ${tr('Thêm chương', 'Add chapter')}`}</button></div>
      </form>}

      <section className="admin-table-card chapter-list">
        <div className="chapter-list-heading"><h3>☷ {tr('Danh sách chương', 'Chapter list')} ({chapters.length})</h3><input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder={tr('Tìm chương...', 'Search chapters...')} /></div>
        <div className="admin-table-wrap">
          <table>
            <thead><tr><th>{tr('TIÊU ĐỀ', 'TITLE')}</th><th>{tr('GIÁ', 'PRICE')}</th><th>{tr('TRANG', 'PAGES')}</th><th>{tr('TRẠNG THÁI', 'STATUS')}</th><th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th></tr></thead>
            <tbody>
              {!filteredChapters.length && <tr><td colSpan="5" className="empty-cell">{search ? tr('Không tìm thấy chương.', 'No chapters found.') : tr('Truyện chưa có chương.', 'This comic has no chapters.')}</td></tr>}
              {filteredChapters.map((item) => (
                <tr key={item.id}>
                  <td><b>{tr('Chương', 'Chapter')} {item.chapterNumber}: {translations[`chapter.${item.id}.title`] || item.title}</b><small className="table-subtitle">{item.slug}</small></td>
                  <td>{item.unitPrice} 🌷</td><td>{item.pageCount}</td>
                  <td><span className={`status-badge ${item.status?.toLowerCase()}`}>{statusLabels[item.status] ? tr(...statusLabels[item.status]) : item.status}</span></td>
                  <td>{isRejected
                    ? <span className="read-only-label">{tr('Chỉ xem', 'Read only')}</span>
                    : <div className="row-actions"><button type="button" className="edit" title={tr('Chỉnh sửa chương', 'Edit chapter')} onClick={() => navigate(`/admin/comics/detail/${slug}/chapters/${item.id}/edit?returnEdit=${editingInfo ? '1' : '0'}`)}>✎ {tr('Sửa', 'Edit')}</button><button type="button" className="delete" title={tr('Xóa chương', 'Delete chapter')} onClick={() => remove(item)}>▣ {tr('Xóa', 'Delete')}</button></div>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>{translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </AdminLayout>
  )
}
