import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import AuthLayout from '../components/AuthLayout'
import FormField from '../components/FormField'
import { LockIcon, MailIcon, UserIcon } from '../components/Icons'
import { register } from '../services/authService'

export default function RegisterPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ username: '', email: '', password: '', confirmPassword: '' })
  const [message, setMessage] = useState(null)
  const [loading, setLoading] = useState(false)

  const update = (event) => setForm({ ...form, [event.target.name]: event.target.value })

  async function submit(event) {
    event.preventDefault()
    setMessage(null)
    if (form.password !== form.confirmPassword) {
      setMessage({ type: 'error', text: 'Mật khẩu xác nhận không khớp.' })
      return
    }
    if (form.password.length < 8) {
      setMessage({ type: 'error', text: 'Mật khẩu phải có ít nhất 8 ký tự.' })
      return
    }

    setLoading(true)
    try {
      await register({ username: form.username, email: form.email, password: form.password })
      navigate(`/check-email?email=${encodeURIComponent(form.email)}`)
    } catch (error) {
      setMessage({ type: 'error', text: error.message })
    } finally {
      setLoading(false)
    }
  }

  return (
    <AuthLayout>
      <header className="form-heading">
        <h1>Đăng ký tài khoản</h1>
        <p>Tham gia cộng đồng Comico ngay hôm nay!</p>
      </header>
      <form onSubmit={submit} className="auth-form">
        <FormField label="Họ và tên" icon={UserIcon} name="username" placeholder="Nguyễn Văn A" value={form.username} onChange={update} autoComplete="name" required />
        <FormField label="Email" icon={MailIcon} name="email" type="email" placeholder="your@email.com" value={form.email} onChange={update} autoComplete="email" required />
        <FormField label="Mật khẩu" icon={LockIcon} name="password" type="password" placeholder="Ít nhất 8 ký tự, có chữ hoa, số và ký tự đặc biệt" value={form.password} onChange={update} autoComplete="new-password" required />
        <FormField label="Xác nhận mật khẩu" icon={LockIcon} name="confirmPassword" type="password" placeholder="Nhập lại mật khẩu" value={form.confirmPassword} onChange={update} autoComplete="new-password" required />
        {message && <p className={`form-message ${message.type}`}>{message.text}</p>}
        <button className="submit-button" disabled={loading}><UserIcon />{loading ? 'Đang đăng ký...' : 'Đăng ký'}</button>
      </form>
      <p className="switch-page">Đã có tài khoản? <Link to="/login">Đăng nhập</Link></p>
    </AuthLayout>
  )
}
