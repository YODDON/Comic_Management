import { useLocation } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'

const sections = {
  '/wallet': ['Ví của tôi', 'Theo dõi số dư và quản lý ví Dâu của bạn.'],
  '/library': ['Thư viện', 'Những truyện bạn đang theo dõi và đã lưu.'],
  '/missions': ['Nhiệm vụ', 'Hoàn thành nhiệm vụ để nhận phần thưởng.'],
  '/transactions': ['Giao dịch', 'Lịch sử nạp, chi tiêu và nhận thưởng.'],
  '/notifications': ['Thông báo', 'Các cập nhật mới nhất dành cho tài khoản của bạn.'],
}

export default function UserSectionPage() {
  const { pathname } = useLocation()
  const [title, description] = sections[pathname] || ['Tài khoản', 'Nội dung đang được cập nhật.']
  return <div className="user-section-page"><SiteHeader /><main><p>TÀI KHOẢN CỦA TÔI</p><h1>{title}</h1><span>{description}</span><section><b>{title}</b><p>Chức năng này sẽ được bổ sung dữ liệu trong bước tiếp theo.</p></section></main></div>
}
