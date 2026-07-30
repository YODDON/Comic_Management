import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import {
  adminToast,
  deleteCategory,
  getCategories,
} from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

export default function AdminCategoriesPage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const location = useLocation()
  const [items, setItems] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState(
    location.state?.message ? { type: 'success', text: location.state.message } : null,
  )
  const translationTexts = useMemo(() => items.flatMap((category) => [
    { key: `category.${category.id}.name`, value: category.name },
    { key: `category.${category.id}.tag`, value: category.tag },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await getCategories({ page, pageSize, search })
      setItems(result.data?.items || [])
      setTotal(result.data?.totalCount || 0)
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load genres.' : error.message })
    } finally {
      setLoading(false)
    }
  }, [language, page, pageSize, search])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  useEffect(() => {
    if (message) {
      adminToast(
        message.type,
        message.type === 'success' ? tr('Thành công', 'Success') : tr('Thất bại', 'Failed'),
        message.text,
      )
    }
  }, [message, tr])

  useEffect(() => {
    if (location.state?.message) window.history.replaceState({}, document.title)
  }, [location.state])

  async function remove(category) {
    if (!window.confirm(tr(`Xóa thể loại “${category.name}”?`, `Delete genre “${translations[`category.${category.id}.name`] || category.name}”?`))) return
    try {
      await deleteCategory(category.id)
      setMessage({ type: 'success', text: tr('Xóa thể loại thành công!', 'Genre deleted successfully!') })
      if (items.length === 1 && page > 1) setPage(page - 1)
      else await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to delete the genre.' : error.message })
    }
  }

  return (
    <AdminLayout title={tr('Quản lý thể loại', 'Genre management')}>
      <div className="admin-page-heading">
        <div>
          <h2>◆ {tr('Quản lý thể loại', 'Genre management')}</h2>
          <p>{tr('Quản lý danh sách thể loại truyện.', 'Manage the list of comic genres.')}</p>
        </div>
        <button onClick={() => navigate('/admin/categories/create')}>＋ {tr('Thêm thể loại', 'Add genre')}</button>
      </div>

      {message && (
        <div className={`admin-alert ${message.type}`}>
          {message.text}
          <button onClick={() => setMessage(null)}>×</button>
        </div>
      )}

      <form
        className="admin-filters admin-list-filters"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
          setSearch(searchInput.trim())
        }}
      >
        <input
          placeholder={tr('Tìm thể loại...', 'Search genres...')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <PageSizeSelect value={pageSize} onChange={(value) => { setPageSize(value); setPage(1) }} />
        <button className="search-button">⌕ {tr('Tìm', 'Search')}</button>
        <button
          type="button"
          className="reset-button"
          onClick={() => {
            setSearchInput('')
            setSearch('')
            setPage(1)
          }}
        >
          {tr('Đặt lại', 'Reset')}
        </button>
      </form>

      <section className="admin-table-card">
        <h3>{tr('Danh sách thể loại', 'Genre list')} ({total})</h3>
        <div className="admin-table-wrap">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>{tr('ĐƯỜNG DẪN (SLUG)', 'SLUG')}</th>
                <th>{tr('TÊN THỂ LOẠI', 'GENRE NAME')}</th>
                <th>TAGS</th>
                <th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan="5" className="empty-cell">{tr('Đang tải dữ liệu...', 'Loading data...')}</td></tr>
              )}
              {!loading && items.length === 0 && (
                <tr><td colSpan="5" className="empty-cell">{tr('Chưa có thể loại phù hợp.', 'No matching genres found.')}</td></tr>
              )}
              {!loading && items.map((category) => (
                <tr key={category.id}>
                  <td><b>#{category.id}</b></td>
                  <td>{category.slug}</td>
                  <td><b>{translations[`category.${category.id}.name`] || category.name}</b></td>
                  <td>{translations[`category.${category.id}.tag`] || category.tag || '-'}</td>
                  <td>
                    <div className="row-actions">
                      <button className="edit" onClick={() => navigate(`/admin/categories/${category.id}/edit`)}>✎ {tr('Sửa', 'Edit')}</button>
                      <button className="delete" onClick={() => remove(category)}>× {tr('Xóa', 'Delete')}</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={page}
          totalItems={total}
          pageSize={pageSize}
          onChange={setPage}
        />
      </section>

      {translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </AdminLayout>
  )
}
