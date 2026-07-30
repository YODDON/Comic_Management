import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { getProfile, logout } from '../services/profileService'
import { getNotifications } from '../services/notificationService'
import { getComic } from '../services/catalogService'
import { getLibrary, updateFollowingComic } from '../services/libraryService'
import { SITE_LANGUAGES, useLanguage } from '../contexts/LanguageContext'
import { hasGuestRoleInToken } from '../services/authService'

const navItems = [
  { to: '/', vi: 'Trang chủ', en: 'Home', icon: 'home' },
  { to: '/search', vi: 'Tìm truyện', en: 'Discover', icon: 'search' },
  { to: '/ranking', vi: 'BXH', en: 'Ranking', icon: 'folder' },
  { to: '/library', vi: 'Thư viện', en: 'Library', icon: 'library' },
]

function HeaderNavIcon({ name }) {
  if (name === 'home') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 11.2 12 4l9 7.2v8.3a.5.5 0 0 1-.5.5H15v-6H9v6H3.5a.5.5 0 0 1-.5-.5z" /></svg>
  if (name === 'search') return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="10.5" cy="10.5" r="6.5" /><path d="m15.5 15.5 5 5" /></svg>
  if (name === 'folder') return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2.5 6.5A1.5 1.5 0 0 1 4 5h6l2 2h8A1.5 1.5 0 0 1 21.5 8.5v10A1.5 1.5 0 0 1 20 20H4a1.5 1.5 0 0 1-1.5-1.5z" /></svg>
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 4h4v16H4zM10 4h4v16h-4zM16.5 4.5l3.5-1 4 15.5-3.5 1z" /></svg>
}

const accountItems = [
  { to: '/profile', vi: 'Trang cá nhân', en: 'Profile', icon: '♟' },
  { to: '/wallet', vi: 'Ví của tôi', en: 'My wallet', icon: '▣' },
  { to: '/library', vi: 'Thư viện', en: 'Library', icon: '▥' },
  { to: '/missions', vi: 'Nhiệm vụ', en: 'Missions', icon: '♛' },
  { to: '/transactions', vi: 'Giao dịch', en: 'Transactions', icon: '▤' },
  { to: '/notifications', vi: 'Thông báo', en: 'Notifications', icon: '♠' },
]

export default function SiteHeader() {
  const { language, locale, setLanguage, tr } = useLanguage()
  const navigate = useNavigate()
  const menuRef = useRef(null)
  const languageRef = useRef(null)
  const loggedIn = Boolean(localStorage.getItem('accessToken'))
  const [profile, setProfile] = useState(null)
  const [open, setOpen] = useState(false)
  const [languageOpen, setLanguageOpen] = useState(false)
  const [adminNotice, setAdminNotice] = useState(null)
  const [chapterNotice, setChapterNotice] = useState(null)
  const [guestNotice, setGuestNotice] = useState(false)
  const shownNoticeRef = useRef(null)
  const isAdmin = profile?.roles?.includes('Admin')
  const isGuest = profile?.roles?.includes('Guest') || hasGuestRoleInToken()
  const selectedLanguage = SITE_LANGUAGES.find((item) => item.value === language)
    || SITE_LANGUAGES[0]

  useEffect(() => {
    const outside = (event) => {
      if (!languageRef.current?.contains(event.target)) setLanguageOpen(false)
    }
    document.addEventListener('mousedown', outside)
    return () => document.removeEventListener('mousedown', outside)
  }, [])

  useEffect(() => {
    if (!loggedIn) return undefined
    const rememberProfile = (value) => {
      setProfile(value)
      if (value?.username) localStorage.setItem('commentUsername', value.username)
    }
    const load = () => getProfile().then((result) => rememberProfile(result.data)).catch(() => {})
    const updated = (event) => rememberProfile(event.detail)
    const outside = (event) => { if (!menuRef.current?.contains(event.target)) setOpen(false) }
    load()
    window.addEventListener('profile-updated', updated)
    document.addEventListener('mousedown', outside)
    return () => {
      window.removeEventListener('profile-updated', updated)
      document.removeEventListener('mousedown', outside)
    }
  }, [loggedIn])

  useEffect(() => {
    if (!loggedIn || !profile || isAdmin || isGuest) return undefined
    const check = () => getNotifications({ pageSize: 10 }).then((result) => {
      const latest = (result.data?.items || []).find((item) => item.userId === 0)
      const dismissedId = localStorage.getItem('dismissedAdminNotificationId')
      if (latest && latest.id !== dismissedId && latest.id !== shownNoticeRef.current) {
        shownNoticeRef.current = latest.id
        setAdminNotice(latest)
      }
    }).catch(() => {})
    check()
    const timer = window.setInterval(check, 5000)
    return () => window.clearInterval(timer)
  }, [loggedIn, profile, isAdmin, isGuest])

  useEffect(() => {
    if (!loggedIn || isAdmin || isGuest) return undefined
    const check = async () => {
      const followed = getLibrary('following')
      for (const saved of followed) {
        try {
          const current = await getComic(saved.slug)
          const previousCount = Number(saved.chapterCount || 0)
          const currentCount = Number(current.chapterCount || 0)
          if (previousCount > 0 && currentCount > previousCount) {
            setChapterNotice({ comic: current, added: currentCount - previousCount })
          }
          updateFollowingComic({ ...current, chapterCount: currentCount })
        } catch { /* A deleted or unavailable comic is skipped. */ }
      }
    }
    check()
    const timer = window.setInterval(check, 30000)
    return () => window.clearInterval(timer)
  }, [loggedIn, isAdmin, isGuest])

  function restrictGuest(event) {
    if (!isGuest) {
      setOpen(false)
      return
    }
    event.preventDefault()
    setOpen(false)
    setGuestNotice(true)
  }

  function dismissAdminNotice() {
    if (adminNotice?.id) localStorage.setItem('dismissedAdminNotificationId', adminNotice.id)
    setAdminNotice(null)
  }

  async function signOut() {
    try { await logout() } catch { /* Clear local session even if the token expired. */ }
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('commentUsername')
    navigate('/login')
  }

  return (
    <><header className="site-header">
      <Link to="/" className="site-logo"><img src="/images/comico-logo.png" alt="Comico" /></Link>
      <nav className="main-nav">
        {navItems.map((item) => <NavLink key={item.to} to={item.to} onClick={item.to === '/library' ? restrictGuest : undefined}><b><HeaderNavIcon name={item.icon} /></b>{tr(item.vi, item.en)}</NavLink>)}
      </nav>
      <div className="header-actions">
        <div className="site-language-menu" ref={languageRef}>
          <button
            type="button"
            className="site-language-select"
            aria-label={tr('Ngôn ngữ hiển thị', 'Display language')}
            aria-expanded={languageOpen}
            onClick={() => setLanguageOpen((value) => !value)}
          >
            <svg className="site-language-globe" viewBox="0 0 24 24" aria-hidden="true">
              <circle cx="12" cy="12" r="9" />
              <path d="M3 12h18M12 3c2.7 2.5 4.2 5.5 4.2 9S14.7 18.5 12 21M12 3C9.3 5.5 7.8 8.5 7.8 12S9.3 18.5 12 21" />
            </svg>
            <span>{selectedLanguage.label}</span>
            <svg className={`site-language-chevron ${languageOpen ? 'open' : ''}`} viewBox="0 0 16 16" aria-hidden="true">
              <path d="m4 6 4 4 4-4" />
            </svg>
          </button>
          {languageOpen && (
            <div className="site-language-options" role="menu">
              {SITE_LANGUAGES.map((item) => (
                <button
                  type="button"
                  role="menuitemradio"
                  aria-checked={language === item.value}
                  className={language === item.value ? 'active' : ''}
                  key={item.value}
                  onClick={() => {
                    setLanguage(item.value)
                    setLanguageOpen(false)
                  }}
                >
                  <span>{item.label}</span>
                  {language === item.value && <b>✓</b>}
                </button>
              ))}
            </div>
          )}
        </div>
        {loggedIn ? (
          <div className="profile-menu" ref={menuRef}>
            <button className="profile-trigger" onClick={() => setOpen((value) => !value)}>
              <span className="header-avatar">{profile?.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : (profile?.username?.[0] || 'C').toUpperCase()}</span>
              <span className="header-username">{profile?.username || tr('Tài khoản', 'Account')}</span><b>⌄</b>
            </button>
            {open && <div className="profile-dropdown"><div className="profile-dropdown-user"><span className="profile-dropdown-avatar">{profile?.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : (profile?.username?.[0] || 'C').toUpperCase()}</span><span><strong>{profile?.username}</strong><small>{profile?.email}</small></span></div>{isAdmin ? <><Link className="profile-dropdown-item" to="/admin/profile" onClick={() => setOpen(false)}><i>♟</i>{tr('Hồ sơ admin', 'Admin profile')}</Link><Link className="profile-dropdown-item" to="/admin/categories" onClick={() => setOpen(false)}><i>▦</i>{tr('Trang quản trị', 'Admin portal')}</Link></> : accountItems.map((item) => <Link className="profile-dropdown-item" key={item.to} to={item.to} onClick={restrictGuest}><i>{item.icon}</i>{tr(item.vi, item.en)}</Link>)}<button className="profile-dropdown-logout" onClick={signOut}>{tr('Đăng xuất', 'Sign out')}</button></div>}
          </div>
        ) : <><Link className="login-outline" to="/login">{tr('Đăng nhập', 'Sign in')}</Link><Link className="register-dark" to="/register">{tr('Đăng ký', 'Register')}</Link></>}
      </div>
    </header>
    {guestNotice && <aside className="guest-access-alert" role="alert" aria-live="assertive"><b>!</b><span>{tr('Tài khoản Guest chỉ được đọc truyện, tìm truyện và xem bảng xếp hạng.', 'Guest accounts can only read comics, discover comics and view rankings.')}</span><button type="button" onClick={() => setGuestNotice(false)} aria-label={tr('Đóng thông báo', 'Close notification')}>×</button></aside>}
    {adminNotice && <aside className="admin-broadcast-popup" role="alert" aria-live="assertive">
      <div className="admin-broadcast-icon">🔔</div>
      <div className="admin-broadcast-content"><small>{tr('THÔNG BÁO TỪ ADMIN', 'ADMIN ANNOUNCEMENT')}</small><h3>{adminNotice.title}</h3><p>{adminNotice.body}</p><time>{new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(new Date(adminNotice.createdAt))}</time></div>
      <button onClick={dismissAdminNotice} aria-label={tr('Đóng thông báo', 'Close notification')}>×</button>
    </aside>}
    {chapterNotice && <aside className="admin-broadcast-popup chapter-release-popup" role="alert" aria-live="polite">
      <div className="admin-broadcast-icon">📖</div>
      <div className="admin-broadcast-content"><small>{tr('TRUYỆN THEO DÕI CÓ CHƯƠNG MỚI', 'NEW CHAPTER AVAILABLE')}</small><h3>{chapterNotice.comic.title}</h3><p>{tr(`Vừa cập nhật ${chapterNotice.added} chương mới. Bấm để xem ngay.`, `${chapterNotice.added} new chapter(s) just arrived. Click to read now.`)}</p><Link to={`/comics/${chapterNotice.comic.slug}`} onClick={() => setChapterNotice(null)}>{tr('Xem truyện →', 'Read now →')}</Link></div>
      <button onClick={() => setChapterNotice(null)} aria-label={tr('Đóng thông báo', 'Close notification')}>×</button>
    </aside>}
    </>
  )
}
