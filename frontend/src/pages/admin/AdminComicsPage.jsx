import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import Pagination from '../../components/Pagination'
import PageSizeSelect from '../../components/PageSizeSelect'
import {
  adminToast,
  getComics,
  toggleOutstanding,
  updateComicStatus,
} from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'
import useTranslatedTexts from '../../hooks/useTranslatedTexts'

const views = {
  approved: {
    vi: 'Truyện đã duyệt', en: 'Approved comics',
    status: 'Ongoing',
    icon: '✓',
    viNote: 'Danh sách truyện đã được duyệt.', enNote: 'List of approved comics.',
  },
  pending: {
    vi: 'Truyện chờ duyệt', en: 'Pending comics',
    status: 'Completed',
    icon: '◷',
    viNote: 'Danh sách truyện đang chờ xét duyệt.', enNote: 'List of comics awaiting review.',
  },
  rejected: {
    vi: 'Truyện bị từ chối', en: 'Rejected comics',
    status: 'Dropped',
    icon: '×',
    viNote: 'Danh sách truyện bị từ chối.', enNote: 'List of rejected comics.',
  },
}

export default function AdminComicsPage() {
  const { language, tr } = useLanguage()
  const { view } = useParams()
  const config = views[view] || views.approved
  const navigate = useNavigate()
  const [items, setItems] = useState([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const translationTexts = useMemo(() => items.flatMap((comic) => [
    { key: `comic.${comic.id}.title`, value: comic.title },
    { key: `comic.${comic.id}.author`, value: comic.authorName },
  ]), [items])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await getComics({
        page,
        pageSize,
        search,
        status: config.status,
      })
      setItems(result.data?.items || [])
      setTotal(result.data?.totalCount || 0)
    } catch (error) {
      adminToast('error', tr('Thất bại', 'Failed'), language === 'en' ? 'Unable to load comics.' : error.message)
    } finally {
      setLoading(false)
    }
  }, [config.status, language, page, pageSize, search, tr])

  useEffect(() => {
    const task = window.setTimeout(load, 0)
    return () => window.clearTimeout(task)
  }, [load])

  useEffect(() => {
    const task = window.setTimeout(() => {
      setPage(1)
      setSearch('')
      setSearchInput('')
    }, 0)
    return () => window.clearTimeout(task)
  }, [view])

  const badge = useMemo(() => ({
    Ongoing: tr('Đã duyệt', 'Approved'),
    Completed: tr('Chờ duyệt', 'Pending'),
    Dropped: tr('Bị từ chối', 'Rejected'),
  }), [tr])

  async function changeStatus(comic, status, successText) {
    try {
      await updateComicStatus(comic.id, status)
      adminToast('success', tr('Thành công', 'Success'), successText)
      if (items.length === 1 && page > 1) setPage(page - 1)
      else await load()
    } catch (error) {
      adminToast('error', tr('Thất bại', 'Failed'), language === 'en' ? 'Unable to update the comic status.' : error.message)
    }
  }

  async function feature(comic) {
    try {
      const result = await toggleOutstanding(comic.id)
      const enabled = Boolean(result?.data?.isOutstanding)
      setItems((current) => current.map((item) =>
        item.id === comic.id ? { ...item, isOutstanding: enabled } : item))
      adminToast(
        'success',
        tr('Thành công', 'Success'),
        enabled
          ? tr(`Đã bật nổi bật cho “${comic.title}”.`, `“${translations[`comic.${comic.id}.title`] || comic.title}” is now featured.`)
          : tr(`Đã tắt nổi bật cho “${comic.title}”.`, `“${translations[`comic.${comic.id}.title`] || comic.title}” is no longer featured.`),
      )
    } catch (error) {
      adminToast('error', tr('Không thể đánh dấu', 'Unable to update featured status'), language === 'en' ? 'Could not update the featured status.' : error.message)
    }
  }

  return (
    <AdminLayout title={tr(config.vi, config.en)}>
      <div className="admin-page-heading">
        <div>
          <h2><span>{config.icon}</span> {tr(config.vi, config.en)}</h2>
          <p>{tr(config.viNote, config.enNote)}</p>
        </div>
        <button onClick={() => navigate('/admin/comics/create')}>＋ {tr('Tạo truyện', 'Create comic')}</button>
      </div>

      <form
        className="admin-filters admin-list-filters"
        onSubmit={(event) => {
          event.preventDefault()
          setPage(1)
          setSearch(searchInput.trim())
        }}
      >
        <input
          placeholder={tr('Tìm theo tên truyện...', 'Search by comic title...')}
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <PageSizeSelect value={pageSize} onChange={(value) => { setPageSize(value); setPage(1) }} />
        <button className="search-button">⌕ {tr('Lọc', 'Filter')}</button>
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
        <h3>{tr(`Danh sách (${total} truyện)`, `Comic list (${total})`)}</h3>
        <div className="admin-table-wrap">
          <table className="comic-admin-table">
            <thead>
              <tr>
                <th>{tr('TRUYỆN', 'COMIC')}</th>
                <th>{tr('TÁC GIẢ', 'AUTHOR')}</th>
                <th>{tr('LƯỢT XEM', 'VIEWS')}</th>
                <th>{tr('TRẠNG THÁI', 'STATUS')}</th>
                <th>{tr('HÀNH ĐỘNG', 'ACTIONS')}</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan="5" className="empty-cell">{tr('Đang tải dữ liệu...', 'Loading data...')}</td></tr>
              )}
              {!loading && !items.length && (
                <tr><td colSpan="5" className="empty-cell">{tr('Không có truyện trong danh sách này.', 'There are no comics in this list.')}</td></tr>
              )}
              {!loading && items.map((comic) => (
                <tr key={comic.id}>
                  <td>
                    <div className="comic-title-cell">
                      {comic.coverUrl
                        ? <img src={comic.coverUrl} alt="" />
                        : <span>▤</span>}
                      <div><b>{translations[`comic.${comic.id}.title`] || comic.title}</b><small>{comic.slug}</small></div>
                    </div>
                  </td>
                  <td>{translations[`comic.${comic.id}.author`] || comic.authorName || '-'}</td>
                  <td>{comic.viewCount || 0}</td>
                  <td>
                    <span className={`status-badge ${comic.status?.toLowerCase()}`}>
                      {badge[comic.status] || comic.status}
                    </span>
                  </td>
                  <td>
                    <div className="row-actions comic-actions">
                      <button
                        title={tr('Xem chi tiết', 'View details')}
                        onClick={() => navigate(`/admin/comics/detail/${comic.slug}`)}
                      >
                        ◉
                      </button>
                      {view !== 'rejected' && (
                        <button
                          className="edit"
                          title={tr('Chỉnh sửa và thêm chương', 'Edit comic and add chapters')}
                          onClick={() => navigate(`/admin/comics/detail/${comic.slug}?edit=1`)}
                        >
                          ✎
                        </button>
                      )}
                      {view === 'approved' && (
                        <button
                          className={`star ${comic.isOutstanding ? 'active' : ''}`}
                          aria-pressed={Boolean(comic.isOutstanding)}
                          title={comic.isOutstanding ? tr('Tắt truyện nổi bật', 'Remove from featured') : tr('Bật truyện nổi bật', 'Mark as featured')}
                          onClick={() => feature(comic)}
                        >
                          ★
                        </button>
                      )}
                      {view === 'pending' && (
                        <button onClick={() => changeStatus(comic, 'Ongoing', tr('Đã duyệt truyện.', 'Comic approved.'))}>
                          ✓ {tr('Duyệt', 'Approve')}
                        </button>
                      )}
                      {view === 'pending' && (
                        <button
                          className="delete"
                          onClick={() => changeStatus(
                            comic,
                            'Dropped',
                            tr('Đã chuyển truyện sang danh sách từ chối.', 'Comic moved to the rejected list.'),
                          )}
                        >
                          × {tr('Từ chối', 'Reject')}
                        </button>
                      )}
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
      </section>{translationError && <div className="global-translation-alert">{translationError}</div>}{translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
    </AdminLayout>
  )
}
