import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import { deleteMission, getAdminMissions } from '../../services/missionService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

const typeLabels = [['Đọc chương truyện', 'Read chapters'], ['Mua chương truyện', 'Unlock chapters'], ['Bình luận', 'Comment'], ['Treo ở sảnh (phút)', 'Lobby time (minutes)']]

export default function AdminMissionsPage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const location = useLocation()
  const [items, setItems] = useState([])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState('all')
  const [statusFilter, setStatusFilter] = useState('all')
  const [loading, setLoading] = useState(true)
  const [message, setMessage] = useState(
    location.state?.message ? { type: 'success', text: location.state.message } : null,
  )
  const translationTexts = useMemo(() => items.flatMap((item) => [
    { key: `mission.${item.id}.title`, value: item.title },
    { key: `mission.${item.id}.description`, value: item.description },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await getAdminMissions()
      setItems(Array.isArray(result?.data) ? result.data : [])
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load missions.' : error.message })
    } finally {
      setLoading(false)
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
    const keyword = search.trim().toLocaleLowerCase('vi')
    return items.filter((item) => {
      const matchesSearch = !keyword
        || item.title?.toLocaleLowerCase('vi').includes(keyword)
        || item.description?.toLocaleLowerCase('vi').includes(keyword)
      const matchesType = typeFilter === 'all' || Number(item.type) === Number(typeFilter)
      const matchesStatus = statusFilter === 'all'
        || (statusFilter === 'active' ? item.isActive : !item.isActive)
      return matchesSearch && matchesType && matchesStatus
    })
  }, [items, search, statusFilter, typeFilter])

  const lastPage = Math.max(1, Math.ceil(filteredItems.length / pageSize))
  const safePage = Math.min(page, lastPage)
  const pagedItems = filteredItems.slice((safePage - 1) * pageSize, safePage * pageSize)

  function resetFilters() {
    setSearchInput('')
    setSearch('')
    setTypeFilter('all')
    setStatusFilter('all')
    setPage(1)
  }

  async function remove(item) {
    if (!window.confirm(tr(`Xóa nhiệm vụ “${item.title}”?`, `Delete mission “${translations[`mission.${item.id}.title`] || item.title}”?`))) return
    try {
      await deleteMission(item.id)
      setMessage({ type: 'success', text: tr('Đã xóa nhiệm vụ.', 'Mission deleted.') })
      await load()
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to delete the mission.' : error.message })
    }
  }

  return <AdminLayout title={tr('Quản lý nhiệm vụ', 'Mission management')}>
    <div className="admin-page-heading">
      <div><h2><span>★</span> {tr('Quản lý nhiệm vụ', 'Mission management')}</h2><p>{tr('Thêm, sửa và xóa nhiệm vụ nhận Dâu.', 'Add, edit and delete missions that reward Dâu.')}</p></div>
      <button onClick={() => navigate('/admin/missions/create')}>＋ {tr('Thêm nhiệm vụ', 'Add mission')}</button>
    </div>
    {message && <div className={`admin-alert ${message.type}`}>{message.text}<button onClick={() => setMessage(null)}>×</button></div>}
    <form
      className="admin-filters mission-admin-filters"
      onSubmit={(event) => {
        event.preventDefault()
        setSearch(searchInput.trim())
        setPage(1)
      }}
    >
      <input
        type="search"
        value={searchInput}
        placeholder={tr('Tìm theo tên hoặc mô tả nhiệm vụ...', 'Search by mission title or description...')}
        aria-label={tr('Tìm kiếm nhiệm vụ', 'Search missions')}
        onChange={(event) => setSearchInput(event.target.value)}
      />
      <select value={typeFilter} aria-label={tr('Lọc theo loại nhiệm vụ', 'Filter by mission type')} onChange={(event) => { setTypeFilter(event.target.value); setPage(1) }}>
        <option value="all">{tr('Tất cả loại', 'All types')}</option>
        {typeLabels.map((label, index) => <option value={index} key={label[0]}>{tr(...label)}</option>)}
      </select>
      <select value={statusFilter} aria-label={tr('Lọc theo trạng thái', 'Filter by status')} onChange={(event) => { setStatusFilter(event.target.value); setPage(1) }}>
        <option value="all">{tr('Tất cả trạng thái', 'All statuses')}</option>
        <option value="active">{tr('Đang hoạt động', 'Active')}</option>
        <option value="inactive">{tr('Đã tắt', 'Inactive')}</option>
      </select>
      <PageSizeSelect value={pageSize} onChange={(value) => { setPageSize(value); setPage(1) }} />
      <button className="search-button">⌕ {tr('Tìm', 'Search')}</button>
      <button type="button" className="reset-button" onClick={resetFilters} disabled={!searchInput && !search && typeFilter === 'all' && statusFilter === 'all'}>{tr('Đặt lại', 'Reset')}</button>
    </form>
    <section className="admin-table-card">
      <h3>{tr('Danh sách nhiệm vụ', 'Mission list')} ({filteredItems.length})</h3>
      <div className="admin-table-wrap"><table><thead><tr>
        <th>{tr('NHIỆM VỤ', 'MISSION')}</th><th>{tr('LOẠI', 'TYPE')}</th><th>{tr('MỤC TIÊU', 'TARGET')}</th><th>{tr('PHẦN THƯỞNG', 'REWARD')}</th><th>{tr('TRẠNG THÁI', 'STATUS')}</th><th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
      </tr></thead><tbody>
        {loading && <tr><td colSpan="6" className="empty-cell">{tr('Đang tải...', 'Loading...')}</td></tr>}
        {!loading && items.length === 0 && <tr><td colSpan="6" className="empty-cell">{tr('Chưa có nhiệm vụ.', 'There are no missions.')}</td></tr>}
        {!loading && items.length > 0 && filteredItems.length === 0 && <tr><td colSpan="6" className="empty-cell">{tr('Không tìm thấy nhiệm vụ phù hợp.', 'No matching missions found.')}</td></tr>}
        {pagedItems.map((item) => <tr key={item.id}>
          <td><b>{translations[`mission.${item.id}.title`] || item.title}</b><small className="mission-table-description">{translations[`mission.${item.id}.description`] || item.description}</small></td>
          <td>{typeLabels[item.type] ? tr(...typeLabels[item.type]) : item.type}</td>
          <td>{item.targetCount}</td>
          <td><b>{item.rewardCoin} Dâu</b></td>
          <td><span className={`mission-status ${item.isActive ? 'active' : ''}`}>{item.isActive ? tr('Hoạt động', 'Active') : tr('Đã xóa/Tắt', 'Deleted/Inactive')}</span></td>
          <td><div className="row-actions">
            <button className="edit" onClick={() => navigate(`/admin/missions/${item.id}/edit`)}>{tr('Sửa', 'Edit')}</button>
            <button className="delete" disabled={!item.isActive} onClick={() => remove(item)}>{tr('Xóa', 'Delete')}</button>
          </div></td>
        </tr>)}
      </tbody></table></div>
      <Pagination page={safePage} totalItems={filteredItems.length} pageSize={pageSize} onChange={setPage} />
    </section>{translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
  </AdminLayout>
}
