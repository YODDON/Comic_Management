import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import { changePassword, getProfile, updateAvatar, updateProfile } from '../services/profileService'
import { useLanguage } from '../contexts/LanguageContext'
import { formatRoleLabels } from '../utils/roleLabels'

const initialPassword = { currentPassword: '', newPassword: '', confirmPassword: '' }

export default function ProfilePage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const fileInput = useRef(null)
  const [profile, setProfile] = useState(null)
  const [username, setUsername] = useState('')
  const [passwords, setPasswords] = useState(initialPassword)
  const [message, setMessage] = useState(null)
  const [busy, setBusy] = useState('')

  useEffect(() => {
    if (!localStorage.getItem('accessToken')) {
      navigate('/login', { replace: true })
      return
    }
    getProfile().then((result) => {
      setProfile(result.data)
      setUsername(result.data.username)
    }).catch((error) => {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to load your profile.' : error.message })
      if (error.message.includes('hết hạn')) navigate('/login', { replace: true })
    })
  }, [language, navigate])

  function publish(updated) {
    setProfile(updated)
    setUsername(updated.username)
    window.dispatchEvent(new CustomEvent('profile-updated', { detail: updated }))
  }

  async function saveProfile(event) {
    event.preventDefault()
    setBusy('profile')
    setMessage(null)
    try {
      const result = await updateProfile(username)
      publish(result.data)
      setMessage({ type: 'success', text: tr('Đã cập nhật tên hiển thị.', 'Display name updated successfully.') })
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update your display name.' : error.message })
    } finally {
      setBusy('')
    }
  }

  async function savePassword(event) {
    event.preventDefault()
    if (passwords.newPassword !== passwords.confirmPassword) {
      setMessage({ type: 'error', text: tr('Mật khẩu xác nhận không khớp.', 'Password confirmation does not match.') })
      return
    }
    setBusy('password')
    setMessage(null)
    try {
      const result = await changePassword(passwords)
      setPasswords(initialPassword)
      setMessage({ type: 'success', text: language === 'en' ? 'Password updated successfully.' : (result.message || 'Đã đổi mật khẩu.') })
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update your password.' : error.message })
    } finally {
      setBusy('')
    }
  }

  async function selectAvatar(event) {
    const file = event.target.files?.[0]
    if (!file) return
    setBusy('avatar')
    setMessage(null)
    try {
      const result = await updateAvatar(file)
      publish(result.data)
      setMessage({ type: 'success', text: tr('Đã cập nhật ảnh đại diện.', 'Profile picture updated successfully.') })
    } catch (error) {
      setMessage({ type: 'error', text: language === 'en' ? 'Unable to update your profile picture.' : error.message })
    } finally {
      setBusy('')
      event.target.value = ''
    }
  }

  const initial = profile?.username?.charAt(0)?.toUpperCase() || 'C'
  const isAdmin = profile?.roles?.includes('Admin')

  return (
    <div className="profile-page">
      <SiteHeader />
      <main className="profile-main">
        <header className="profile-title"><p>{tr('TÀI KHOẢN CỦA TÔI', 'MY ACCOUNT')}</p><h1>{tr('Quản lý hồ sơ', 'Profile settings')}</h1><span>{tr('Cập nhật thông tin cá nhân và bảo mật tài khoản Comico.', 'Update your personal information and protect your Comico account.')}</span></header>
        {message && <div className={`profile-alert ${message.type}`}>{message.text}</div>}
        <div className={`profile-layout ${isAdmin ? 'admin-avatar-profile' : ''}`}>
          <aside className="profile-card identity-card">
            <div className="avatar-large">
              {profile?.avatarUrl ? <img src={profile.avatarUrl} alt={profile.username} /> : <span>{initial}</span>}
              {busy === 'avatar' && <i>{tr('Đang tải...', 'Uploading...')}</i>}
            </div>
            <h2>{profile?.username || tr('Đang tải...', 'Loading...')}</h2>
            <p>{profile?.email}</p>
            <button onClick={() => fileInput.current?.click()} disabled={busy === 'avatar'}>{tr('Thay ảnh đại diện', 'Change profile picture')}</button>
            <input ref={fileInput} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={selectAvatar} />
            <small>{tr('JPG, PNG hoặc WEBP · tối đa 5 MB', 'JPG, PNG or WEBP · up to 5 MB')}</small>
            <div className="account-meta"><span>{tr('Vai trò', 'Role')}</span><b>{formatRoleLabels(profile?.roles)}</b></div>
            <div className="account-meta"><span>{tr('Xác thực email', 'Email verification')}</span><b className={profile?.isEmailVerified ? 'verified' : ''}>{profile?.isEmailVerified ? tr('Đã xác thực', 'Verified') : tr('Chưa xác thực', 'Not verified')}</b></div>
          </aside>

          {!isAdmin && <div className="profile-forms">
            <form className="profile-card settings-card" onSubmit={saveProfile}>
              <div className="card-heading"><div><h2>{tr('Thông tin cá nhân', 'Personal information')}</h2><p>{tr('Thông tin này sẽ hiển thị trên Comico.', 'This information will be displayed on Comico.')}</p></div><span>✎</span></div>
              <label>{tr('Tên hiển thị', 'Display name')}<input value={username} onChange={(event) => setUsername(event.target.value)} minLength="2" maxLength="100" required /></label>
              <label>Email<input value={profile?.email || ''} disabled /><small>{tr('Email không thể thay đổi tại đây.', 'Email cannot be changed here.')}</small></label>
              <button className="pink-button" disabled={busy === 'profile'}>{busy === 'profile' ? tr('Đang lưu...', 'Saving...') : tr('Lưu thay đổi', 'Save changes')}</button>
            </form>

            <form className="profile-card settings-card" onSubmit={savePassword}>
              <div className="card-heading"><div><h2>{tr('Đổi mật khẩu', 'Change password')}</h2><p>{tr('Dùng mật khẩu mạnh để bảo vệ tài khoản.', 'Use a strong password to protect your account.')}</p></div><span>⌁</span></div>
              <label>{tr('Mật khẩu hiện tại', 'Current password')}<input type="password" value={passwords.currentPassword} onChange={(event) => setPasswords({ ...passwords, currentPassword: event.target.value })} autoComplete="current-password" required /></label>
              <div className="password-row">
                <label>{tr('Mật khẩu mới', 'New password')}<input type="password" value={passwords.newPassword} onChange={(event) => setPasswords({ ...passwords, newPassword: event.target.value })} autoComplete="new-password" required /></label>
                <label>{tr('Xác nhận mật khẩu', 'Confirm password')}<input type="password" value={passwords.confirmPassword} onChange={(event) => setPasswords({ ...passwords, confirmPassword: event.target.value })} autoComplete="new-password" required /></label>
              </div>
              <small>{tr('Tối thiểu 8 ký tự, gồm chữ hoa, số và ký tự đặc biệt.', 'At least 8 characters, including an uppercase letter, a number and a special character.')}</small>
              <button className="dark-button" disabled={busy === 'password'}>{busy === 'password' ? tr('Đang cập nhật...', 'Updating...') : tr('Cập nhật mật khẩu', 'Update password')}</button>
            </form>
          </div>}
        </div>
      </main>
    </div>
  )
}
