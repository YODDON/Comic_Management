import { useEffect, useRef, useState } from 'react'
import AdminLayout from '../../components/AdminLayout'
import { getProfile, updateAvatar } from '../../services/profileService'
import { adminToast } from '../../services/adminService'
import { useLanguage } from '../../contexts/LanguageContext'

export default function AdminProfilePage() {
  const { language, tr } = useLanguage()
  const fileInput = useRef(null)
  const [profile, setProfile] = useState(null)
  const [message, setMessage] = useState(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => { getProfile().then((result) => setProfile(result.data)).catch((error) => setMessage({ type: 'error', text: language === 'en' ? 'Unable to load the admin profile.' : error.message })) }, [language])

  async function selectAvatar(event) {
    const file = event.target.files?.[0]
    if (!file) return
    setBusy(true); setMessage(null)
    try {
      const result = await updateAvatar(file)
      setProfile(result.data)
      window.dispatchEvent(new CustomEvent('profile-updated', { detail: result.data }))
      const successMessage = tr('Đã cập nhật ảnh đại diện.', 'Profile picture updated successfully.')
      setMessage({ type: 'success', text: successMessage }); adminToast('success', tr('Thành công', 'Success'), successMessage)
    } catch (error) { const errorMessage = language === 'en' ? 'Unable to update the profile picture.' : error.message; setMessage({ type: 'error', text: errorMessage }); adminToast('error', tr('Cập nhật thất bại', 'Update failed'), errorMessage) }
    finally { setBusy(false); event.target.value = '' }
  }

  const initial = profile?.username?.charAt(0)?.toUpperCase() || 'A'
  return <AdminLayout title={tr('Hồ sơ admin', 'Admin profile')}>
    <div className="admin-page-heading"><div><h2>{tr('Hồ sơ admin', 'Admin profile')}</h2><p>{tr('Quản lý ảnh đại diện của tài khoản quản trị.', 'Manage the administrator account profile picture.')}</p></div></div>
    {message && <div className={`admin-alert ${message.type}`}>{message.text}</div>}
    <aside className="profile-card identity-card admin-profile-card">
      <div className="avatar-large">{profile?.avatarUrl ? <img src={profile.avatarUrl} alt={profile.username} /> : <span>{initial}</span>}{busy && <i>{tr('Đang tải...', 'Uploading...')}</i>}</div>
      <h2>{profile?.username || 'Admin'}</h2><p>{profile?.email}</p>
      <button onClick={() => fileInput.current?.click()} disabled={busy}>{tr('Thay ảnh đại diện', 'Change profile picture')}</button>
      <input ref={fileInput} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={selectAvatar} />
      <small>{tr('JPG, PNG hoặc WEBP · tối đa 5 MB', 'JPG, PNG or WEBP · up to 5 MB')}</small>
      <div className="account-meta"><span>ID</span><b>#{profile?.id || '-'}</b></div>
      <div className="account-meta"><span>{tr('Vai trò', 'Role')}</span><b>{profile?.roles?.join(', ') || 'Admin'}</b></div>
    </aside>
  </AdminLayout>
}
