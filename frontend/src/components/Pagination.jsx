import { useLanguage } from '../contexts/LanguageContext'

export default function Pagination({
  page,
  totalItems,
  pageSize = 10,
  onChange,
}) {
  const { tr } = useLanguage()
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))
  if (totalItems <= pageSize) return null

  const start = Math.max(1, Math.min(page - 2, totalPages - 4))
  const end = Math.min(totalPages, start + 4)
  const pages = []
  for (let value = start; value <= end; value += 1) pages.push(value)

  return (
    <nav className="app-pagination" aria-label={tr('Phân trang', 'Pagination')}>
      <button type="button" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        ← {tr('Trước', 'Previous')}
      </button>
      {pages.map((value) => (
        <button
          type="button"
          key={value}
          className={value === page ? 'active' : ''}
          aria-current={value === page ? 'page' : undefined}
          onClick={() => onChange(value)}
        >
          {value}
        </button>
      ))}
      <button type="button" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
        {tr('Sau', 'Next')} →
      </button>
      <span>{tr('Trang', 'Page')} {page}/{totalPages}</span>
    </nav>
  )
}
