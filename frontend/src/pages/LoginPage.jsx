import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import AuthLayout from '../components/AuthLayout'
import FormField from '../components/FormField'
import { LockIcon, MailIcon, UserIcon } from '../components/Icons'
import { login } from '../services/authService'
import { getProfile } from '../services/profileService'

export default function LoginPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ email: '', password: '' })
  const [message, setMessage] = useState(null)
  const [loading, setLoading] = useState(false)

  const update = (event) => setForm({ ...form, [event.target.name]: event.target.value })

  async function submit(event) {
    event.preventDefault()
    setLoading(true)
    setMessage(null)
    try {
      const result = await login(form)
      localStorage.setItem('accessToken', result.data.accessToken)
      localStorage.setItem('refreshToken', result.data.refreshToken)
      const profile = await getProfile()
      const isAdmin = profile.data.roles?.includes('Admin')
      navigate(isAdmin ? '/admin/dashboard' : '/', { replace: true })
    } catch (error) {
      setMessage({ type: 'error', text: error.message })
    } finally {
      setLoading(false)
    }
  }

  return (
    <AuthLayout>
      <header className="form-heading"><h1>Đăng nhập</h1><p>Chào mừng bạn quay trở lại!</p></header>
      <form onSubmit={submit} className="auth-form">
        <FormField label="Email" icon={MailIcon} name="email" type="email" placeholder="admin@comic.com" value={form.email} onChange={update} autoComplete="email" required />
        <FormField label="Mật khẩu" icon={LockIcon} name="password" type="password" placeholder="••••••••" value={form.password} onChange={update} autoComplete="current-password" required />
        {message && <p className={`form-message ${message.type}`}>{message.text}</p>}
        {message?.type === 'error' && message.text.includes('chưa được xác nhận') && <Link className="unverified-link" to={`/check-email?email=${encodeURIComponent(form.email)}`}>Gửi lại email xác nhận</Link>}
        <button className="submit-button" disabled={loading}><UserIcon />{loading ? 'Đang đăng nhập...' : 'Đăng nhập'}</button>
      </form>
      <div className="login-footer-links"><Link to="/forgot-password">Quên mật khẩu?</Link><span>Chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link></span></div>
    </AuthLayout>
  )
}
