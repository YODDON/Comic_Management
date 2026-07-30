import { useLocation } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { useLanguage } from '../../contexts/LanguageContext'

const names = {
  '/admin/dashboard': ['Bảng điều khiển', 'Dashboard'], '/admin/comics/approved': ['Truyện đã duyệt', 'Approved comics'], '/admin/comics/pending': ['Truyện chờ duyệt', 'Pending comics'], '/admin/comics/rejected': ['Truyện bị từ chối', 'Rejected comics'],
  '/admin/deposits': ['Quản lý nạp tiền', 'Deposit management'], '/admin/banners': ['Quản lý Banner', 'Banner management'], '/admin/users': ['Người dùng', 'Users'],
  '/admin/missions': ['Nhiệm vụ', 'Missions'], '/admin/notifications': ['Thông báo', 'Notifications'],
}

export default function AdminPlaceholderPage() {
  const { pathname } = useLocation()
  const { tr } = useLanguage()
  const name = names[pathname] || ['Quản trị Comico', 'Comico administration']
  const title = tr(...name)
  return <AdminLayout title={title}><section className="admin-placeholder"><span>✦</span><h2>{title}</h2><p>{tr('Giao diện chức năng này sẽ được bổ sung ở bước tiếp theo.', 'This feature will be added in a future update.')}</p></section></AdminLayout>
}
