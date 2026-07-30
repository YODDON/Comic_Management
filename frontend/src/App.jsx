import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import HomePage from './pages/HomePage'
import ProfilePage from './pages/ProfilePage'
import CheckEmailPage from './pages/CheckEmailPage'
import VerifyEmailPage from './pages/VerifyEmailPage'
import ForgotPasswordPage from './pages/ForgotPasswordPage'
import ResetPasswordPage from './pages/ResetPasswordPage'
import AdminRoute from './components/AdminRoute'
import MemberRoute from './components/MemberRoute'
import AdminCategoriesPage from './pages/admin/AdminCategoriesPage'
import AdminCategoryCreatePage from './pages/admin/AdminCategoryCreatePage'
import AdminCategoryEditPage from './pages/admin/AdminCategoryEditPage'
import AdminProfilePage from './pages/admin/AdminProfilePage'
import AdminComicsPage from './pages/admin/AdminComicsPage'
import AdminComicCreatePage from './pages/admin/AdminComicCreatePage'
import AdminComicDetailPage from './pages/admin/AdminComicDetailPage'
import AdminChapterEditPage from './pages/admin/AdminChapterEditPage'
import AdminPlaceholderPage from './pages/admin/AdminPlaceholderPage'
import MissionsPage from './pages/MissionsPage'
import AdminMissionsPage from './pages/admin/AdminMissionsPage'
import AdminMissionCreatePage from './pages/admin/AdminMissionCreatePage'
import AdminMissionEditPage from './pages/admin/AdminMissionEditPage'
import NotificationsPage from './pages/NotificationsPage'
import AdminNotificationsPage from './pages/admin/AdminNotificationsPage'
import AdminNotificationCreatePage from './pages/admin/AdminNotificationCreatePage'
import AdminNotificationEditPage from './pages/admin/AdminNotificationEditPage'
import ComicSearchPage from './pages/ComicSearchPage'
import ComicDetailPage from './pages/ComicDetailPage'
import ChapterReaderPage from './pages/ChapterReaderPage'
import RankingPage from './pages/RankingPage'
import LibraryPage from './pages/LibraryPage'
import AdminBannersPage from './pages/admin/AdminBannersPage'
import AdminBannerCreatePage from './pages/admin/AdminBannerCreatePage'
import AdminBannerEditPage from './pages/admin/AdminBannerEditPage'
import AdminDashboardPage from './pages/admin/AdminDashboardPage'
import AdminUsersPage from './pages/admin/AdminUsersPage'
import AdminUserDetailPage from './pages/admin/AdminUserDetailPage'
import AdminDepositsPage from './pages/admin/AdminDepositsPage'
import ComicCollectionPage from './pages/ComicCollectionPage'
import WalletPage from './pages/WalletPage'
import DepositPage from './pages/DepositPage'
import TransactionHistoryPage from './pages/TransactionHistoryPage'
import { hasAdminRoleInToken } from './services/authService'

function HomeEntry() {
  return hasAdminRoleInToken()
    ? <Navigate to="/admin/dashboard" replace />
    : <HomePage />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/" element={<HomeEntry />} />
      <Route path="/search" element={<ComicSearchPage />} />
      <Route path="/categories" element={<ComicSearchPage />} />
      <Route path="/ranking" element={<RankingPage />} />
      <Route path="/comics/:slug" element={<ComicDetailPage />} />
      <Route path="/collections/:type" element={<ComicCollectionPage />} />
      <Route path="/read/:comicId/:chapterSlug" element={<ChapterReaderPage />} />
      <Route path="/profile" element={<MemberRoute><ProfilePage /></MemberRoute>} />
      <Route path="/wallet" element={<MemberRoute><WalletPage /></MemberRoute>} />
      <Route path="/wallet/deposit" element={<MemberRoute><DepositPage /></MemberRoute>} />
      <Route path="/wallet/transactions" element={<MemberRoute><TransactionHistoryPage /></MemberRoute>} />
      <Route path="/library" element={<MemberRoute><LibraryPage /></MemberRoute>} />
      <Route path="/missions" element={<MemberRoute><MissionsPage /></MemberRoute>} />
      <Route path="/transactions" element={<MemberRoute><TransactionHistoryPage /></MemberRoute>} />
      <Route path="/notifications" element={<MemberRoute><NotificationsPage /></MemberRoute>} />
      <Route path="/check-email" element={<CheckEmailPage />} />
      <Route path="/verify-email" element={<VerifyEmailPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/admin" element={<Navigate to="/admin/dashboard" replace />} />
      <Route path="/admin/dashboard" element={<AdminRoute><AdminDashboardPage /></AdminRoute>} />
      <Route path="/admin/users" element={<AdminRoute><AdminUsersPage /></AdminRoute>} />
      <Route path="/admin/withdrawals" element={<Navigate to="/admin/dashboard" replace />} />
      <Route path="/admin/deposits" element={<AdminRoute><AdminDepositsPage /></AdminRoute>} />
      <Route path="/admin/users/:id" element={<AdminRoute><AdminUserDetailPage /></AdminRoute>} />
      <Route path="/admin/categories" element={<AdminRoute><AdminCategoriesPage /></AdminRoute>} />
      <Route path="/admin/categories/create" element={<AdminRoute><AdminCategoryCreatePage /></AdminRoute>} />
      <Route path="/admin/categories/:id/edit" element={<AdminRoute><AdminCategoryEditPage /></AdminRoute>} />
      <Route path="/admin/profile" element={<AdminRoute><AdminProfilePage /></AdminRoute>} />
      <Route path="/admin/missions" element={<AdminRoute><AdminMissionsPage /></AdminRoute>} />
      <Route path="/admin/missions/create" element={<AdminRoute><AdminMissionCreatePage /></AdminRoute>} />
      <Route path="/admin/missions/:id/edit" element={<AdminRoute><AdminMissionEditPage /></AdminRoute>} />
      <Route path="/admin/notifications" element={<AdminRoute><AdminNotificationsPage /></AdminRoute>} />
      <Route path="/admin/notifications/create" element={<AdminRoute><AdminNotificationCreatePage /></AdminRoute>} />
      <Route path="/admin/notifications/:id/edit" element={<AdminRoute><AdminNotificationEditPage /></AdminRoute>} />
      <Route path="/admin/banners" element={<AdminRoute><AdminBannersPage /></AdminRoute>} />
      <Route path="/admin/banners/create" element={<AdminRoute><AdminBannerCreatePage /></AdminRoute>} />
      <Route path="/admin/banners/:id/edit" element={<AdminRoute><AdminBannerEditPage /></AdminRoute>} />
      <Route path="/admin/comics/create" element={<AdminRoute><AdminComicCreatePage /></AdminRoute>} />
      <Route path="/admin/comics/detail/:slug" element={<AdminRoute><AdminComicDetailPage /></AdminRoute>} />
      <Route path="/admin/comics/detail/:slug/chapters/:chapterId/edit" element={<AdminRoute><AdminChapterEditPage /></AdminRoute>} />
      <Route path="/admin/comics/:view" element={<AdminRoute><AdminComicsPage /></AdminRoute>} />
      <Route path="/admin/*" element={<AdminRoute><AdminPlaceholderPage /></AdminRoute>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
