import { Link } from 'react-router-dom'
import SiteHeader from './SiteHeader'
import { hasGuestRoleInToken } from '../services/authService'
import { useLanguage } from '../contexts/LanguageContext'

export default function MemberRoute({ children }) {
  const { tr } = useLanguage()

  if (!hasGuestRoleInToken()) return children

  return (
    <div className="guest-restricted-page">
      <SiteHeader />
      <main>
        <div className="guest-restricted-icon">!</div>
        <h1>{tr('Chức năng không dành cho Guest', 'This feature is unavailable to Guests')}</h1>
        <p>{tr(
          'Tài khoản Guest chỉ được đọc truyện, tìm truyện và xem bảng xếp hạng.',
          'Guest accounts can only read comics, discover comics and view rankings.',
        )}</p>
        <Link to="/">{tr('Quay về trang chủ', 'Return home')}</Link>
      </main>
    </div>
  )
}
