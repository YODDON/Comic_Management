import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import { deleteBanner, getAdminBanners, updateBanner } from '../../services/bannerService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

const formatDate = (value, locale) => new Intl.DateTimeFormat(locale, {
  dateStyle: 'short',
  timeStyle: 'short',
}).format(new Date(value))

export default function AdminBannersPage() {
  const { language, locale, tr } = useLanguage()
  const navigate = useNavigate()
  const location = useLocation()
  const [items, setItems] = useState([])
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('all')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [message, setMessage] = useState(
    location.state?.message ? { type: 'success', text: location.state.message } : null,
  )
  const translationTexts = useMemo(() => items.map((item) => ({ key: `banner.${item.id}.title`, value: item.title })), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  const load = useCallback(async () => {
    try {
      setItems(await getAdminBanners() || [])
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load banners.' : error.message })
    }
  }, [language])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  useEffect(() => {
    if (location.state?.message) window.history.replaceState({}, document.title)
  }, [location.state])

  const filteredItems = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase('vi-VN')
    return items.filter((item) => {
      const matchesSearch = !keyword
        || item.title?.toLocaleLowerCase('vi-VN').includes(keyword)
        || item.linkUrl?.toLocaleLowerCase('vi-VN').includes(keyword)
      const matchesStatus = status === 'all'
        || (status === 'active' ? item.isActive : !item.isActive)
      return matchesSearch && matchesStatus
    })
  }, [items, search, status])

  const lastPage = Math.max(1, Math.ceil(filteredItems.length / pageSize))
  const safePage = Math.min(page, lastPage)
  const pagedItems = filteredItems.slice((safePage - 1) * pageSize, safePage * pageSize)

  function submitFilters(event) {
    event.preventDefault()
    setSearch(searchInput.trim())
    setPage(1)
  }

  function resetFilters() {
    setSearchInput('')
    setSearch('')
    setStatus('all')
    setPageSize(10)
    setPage(1)
  }

  async function remove(item) {
    if (!window.confirm(tr(`Xóa banner “${item.title || 'Không có tiêu đề'}”?`, `Delete banner “${translations[`banner.${item.id}.title`] || item.title || 'Untitled'}”?`))) return
    try {
      await deleteBanner(item.id)
      setMessage({ type: 'success', text: tr('Đã xóa banner.', 'Banner deleted.') })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to delete the banner.' : error.message })
    }
  }

  async function toggle(item) {
    try {
      await updateBanner(item.id, {
        title: item.title || null,
        imageUrl: item.imageUrl,
        linkUrl: item.linkUrl || null,
        displayOrder: item.displayOrder,
        isActive: !item.isActive,
      })
      setMessage({
        type: 'success',
        text: item.isActive ? tr('Đã ẩn banner khỏi trang chủ.', 'Banner hidden from the home page.') : tr('Đã hiển thị banner trên trang chủ.', 'Banner displayed on the home page.'),
      })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update the banner status.' : error.message })
    }
  }

  return (
    <AdminLayout title={tr('Quản lý Banner', 'Banner management')}>
      <div className="admin-page-heading">
        <div>
          <h2>▣ {tr('Quản lý Banner', 'Banner management')}</h2>
          <p>{tr('Thêm, sửa, xóa và sắp xếp banner hiển thị trên trang chủ.', 'Add, edit, delete and order banners displayed on the home page.')}</p>
        </div>
        <button onClick={() => navigate('/admin/banners/create')}>＋ {tr('Thêm banner', 'Add banner')}</button>
      </div>

      {message && (
        <div className={`admin-alert ${message.type}`}>
          {message.text}
          <button onClick={() => setMessage(null)}>×</button>
        </div>
      )}

      <form className="admin-filters admin-list-filters" onSubmit={submitFilters}>
        <input
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder={tr('Tìm theo tiêu đề hoặc liên kết...', 'Search by title or link...')}
        />
        <select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }}>
          <option value="all">{tr('Tất cả trạng thái', 'All statuses')}</option>
          <option value="active">{tr('Đang hiển thị', 'Visible')}</option>
          <option value="inactive">{tr('Đã ẩn', 'Hidden')}</option>
        </select>
        <PageSizeSelect
          value={pageSize}
          onChange={(value) => { setPageSize(value); setPage(1) }}
        />
        <button className="search-button" type="submit">⌕ {tr('Tìm', 'Search')}</button>
        <button className="reset-button" type="button" onClick={resetFilters}>{tr('Đặt lại', 'Reset')}</button>
      </form>

      <section className="admin-table-card banner-admin-list">
        <h3>{tr('Danh sách banner', 'Banner list')} ({filteredItems.length})</h3>
        <div className="admin-table-wrap">
          <table>
            <thead>
              <tr>
                <th>{tr('ẢNH', 'IMAGE')}</th>
                <th>{tr('TIÊU ĐỀ', 'TITLE')}</th>
                <th>{tr('THỨ TỰ', 'ORDER')}</th>
                <th>{tr('TRẠNG THÁI', 'STATUS')}</th>
                <th>{tr('NGÀY TẠO', 'CREATED')}</th>
                <th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
              </tr>
            </thead>
            <tbody>
              {pagedItems.length === 0 ? (
                <tr><td colSpan="6" className="empty-cell">{tr('Không tìm thấy banner phù hợp.', 'No matching banners found.')}</td></tr>
              ) : pagedItems.map((item) => (
                <tr key={item.id}>
                  <td><img className="banner-table-image" src={item.imageUrl} alt="" /></td>
                  <td>
                    <b>{translations[`banner.${item.id}.title`] || item.title || tr('Không có tiêu đề', 'Untitled')}</b>
                    <small className="banner-target">{item.linkUrl || tr('Không có liên kết', 'No link')}</small>
                  </td>
                  <td>{item.displayOrder}</td>
                  <td>
                    <button
                      className={`notification-status ${item.isActive ? 'active' : ''}`}
                      onClick={() => toggle(item)}
                    >
                      {item.isActive ? tr('Đang hiển thị', 'Visible') : tr('Đã ẩn', 'Hidden')}
                    </button>
                  </td>
                  <td>{formatDate(item.createdAt, locale)}</td>
                  <td>
                    <div className="row-actions">
                      <button className="edit" onClick={() => navigate(`/admin/banners/${item.id}/edit`)}>{tr('Sửa', 'Edit')}</button>
                      <button className="delete" onClick={() => remove(item)}>{tr('Xóa', 'Delete')}</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={safePage}
          totalItems={filteredItems.length}
          pageSize={pageSize}
          onChange={setPage}
        />
      </section>{translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </AdminLayout>
  )
}
