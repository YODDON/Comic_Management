import { useLanguage } from '../contexts/LanguageContext'

const options = [10, 20, 30, 40, 50]

export default function PageSizeSelect({ value, onChange }) {
  const { tr } = useLanguage()
  return (
    <label className="page-size-field">
      <span>{tr('Hiển thị', 'Show')}</span>
      <select value={value} onChange={(event) => onChange(Number(event.target.value))}>
        {options.map((option) => (
          <option key={option} value={option}>{option} {tr('mục / trang', 'items / page')}</option>
        ))}
      </select>
    </label>
  )
}
