import { useEffect, useRef, useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { getProfile, logout } from '../services/profileService'
import { SITE_LANGUAGES, useLanguage } from '../contexts/LanguageContext'

const groups = [
  { vi: 'TỔNG QUAN', en: 'OVERVIEW', items: [{ icon: '◉', vi: 'Bảng điều khiển', en: 'Dashboard', to: '/admin/dashboard' }] },
  { vi: 'QUẢN LÝ TRUYỆN', en: 'COMIC MANAGEMENT', items: [
    { icon: '✓', vi: 'Truyện đã duyệt', en: 'Approved comics', to: '/admin/comics/approved' },
    { icon: '◷', vi: 'Truyện chờ duyệt', en: 'Pending comics', to: '/admin/comics/pending' },
    { icon: '×', vi: 'Truyện bị từ chối', en: 'Rejected comics', to: '/admin/comics/rejected' },
    { icon: '◆', vi: 'Thể loại truyện', en: 'Comic genres', to: '/admin/categories' },
  ] },
  { vi: 'TÀI CHÍNH', en: 'FINANCE', items: [
    { icon: '$', vi: 'Quản lý nạp tiền', en: 'Deposit management', to: '/admin/deposits' },
  ] },
  { vi: 'GIAO DIỆN', en: 'APPEARANCE', items: [{ icon: '▣', vi: 'Quản lý Banner', en: 'Banner management', to: '/admin/banners' }] },
  { vi: 'HỆ THỐNG', en: 'SYSTEM', items: [
    { icon: '♣', vi: 'Người dùng', en: 'Users', to: '/admin/users' },
    { icon: '♛', vi: 'Nhiệm vụ', en: 'Missions', to: '/admin/missions' },
    { icon: '♠', vi: 'Thông báo', en: 'Notifications', to: '/admin/notifications' },
  ] },
]

export default function AdminLayout({ title, children }) {
  const { language, setLanguage, tr } = useLanguage()
  const navigate = useNavigate()
  const languageRef = useRef(null)
  const [profile, setProfile] = useState(null)
  const [collapsed, setCollapsed] = useState(false)
  const [menu, setMenu] = useState(false)
  const [toast, setToast] = useState(null)
  const [languageOpen, setLanguageOpen] = useState(false)
  const selectedLanguage = SITE_LANGUAGES.find((item) => item.value === language) || SITE_LANGUAGES[0]

  useEffect(() => { getProfile().then((result) => setProfile(result.data)).catch(() => {}) }, [])
  useEffect(() => {
    const show = (event) => { setToast(event.detail); window.clearTimeout(window.__adminToastTimer); window.__adminToastTimer = window.setTimeout(() => setToast(null), 4000) }
    window.addEventListener('admin-toast', show)
    return () => window.removeEventListener('admin-toast', show)
  }, [])
  useEffect(() => {
    const outside = (event) => {
      if (!languageRef.current?.contains(event.target)) setLanguageOpen(false)
    }
    document.addEventListener('mousedown', outside)
    return () => document.removeEventListener('mousedown', outside)
  }, [])

  async function signOut() {
    try { await logout() } catch { /* local logout remains valid */ }
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    navigate('/login')
  }

  return (
    <div className={`admin-shell ${collapsed ? 'collapsed' : ''}`}>
      <aside className="admin-sidebar">
        <div className="admin-brand"><img src="/images/comico-logo.png" alt="Comico Admin" /><button onClick={() => setCollapsed((value) => !value)}>‹</button></div>
        <div className="admin-scroll">
          {groups.map((group) => <section className="admin-nav-group" key={group.vi}><p>{tr(group.vi, group.en)}</p>{group.items.map((item) => <NavLink key={item.to} to={item.to} title={tr(item.vi, item.en)}><i>{item.icon}</i><span>{tr(item.vi, item.en)}</span></NavLink>)}</section>)}
        </div>
      </aside>
      <div className="admin-workspace">
        <header className="admin-topbar"><h1>{title}</h1><div className="admin-topbar-actions"><div className="site-language-menu" ref={languageRef}><button type="button" className="site-language-select" aria-label={tr('Ngôn ngữ hiển thị', 'Display language')} aria-expanded={languageOpen} onClick={() => setLanguageOpen((value) => !value)}><svg className="site-language-globe" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9" /><path d="M3 12h18M12 3c2.7 2.5 4.2 5.5 4.2 9S14.7 18.5 12 21M12 3C9.3 5.5 7.8 8.5 7.8 12S9.3 18.5 12 21" /></svg><span>{selectedLanguage.label}</span><svg className={`site-language-chevron ${languageOpen ? 'open' : ''}`} viewBox="0 0 16 16" aria-hidden="true"><path d="m4 6 4 4 4-4" /></svg></button>{languageOpen && <div className="site-language-options" role="menu">{SITE_LANGUAGES.map((item) => <button type="button" role="menuitemradio" aria-checked={language === item.value} className={language === item.value ? 'active' : ''} key={item.value} onClick={() => { setLanguage(item.value); setLanguageOpen(false) }}><span>{item.label}</span>{language === item.value && <b>✓</b>}</button>)}</div>}</div><div className="admin-account"><button onClick={() => setMenu((value) => !value)}><span className="admin-avatar">{profile?.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : (profile?.username?.[0] || 'A')}</span><span><b>{profile?.username || 'Admin'}</b><small>ADMIN</small></span><i>⌄</i></button>{menu && <div><a href="/admin/profile">{tr('Hồ sơ cá nhân', 'Profile')}</a><button onClick={signOut}>{tr('Đăng xuất', 'Sign out')}</button></div>}</div></div></header>
        <main className="admin-content">{children}</main>
        {toast && <div className={`admin-toast ${toast.type || 'success'}`}><span>{toast.type === 'error' ? '!' : '✓'}</span><div><b>{toast.title}</b><p>{toast.text}</p></div><button onClick={() => setToast(null)}>×</button></div>}
      </div>
    </div>
  )
}
