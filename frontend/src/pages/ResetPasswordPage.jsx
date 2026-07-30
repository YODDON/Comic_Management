import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import AuthLayout from '../components/AuthLayout'
import FormField from '../components/FormField'
import { LockIcon } from '../components/Icons'
import { resetPassword } from '../services/authService'

export default function ResetPasswordPage() {
  const [params] = useSearchParams()
  const token = params.get('token') || ''
  const [form, setForm] = useState({ password: '', confirm: '' })
  const [message, setMessage] = useState(null)
  const [loading, setLoading] = useState(false)
  async function submit(event) {
    event.preventDefault()
    if (!token) return setMessage({ type: 'error', text: 'Liên kết đặt lại mật khẩu không hợp lệ.' })
    if (form.password !== form.confirm) return setMessage({ type: 'error', text: 'Mật khẩu xác nhận không khớp.' })
    setLoading(true); setMessage(null)
    try { await resetPassword(token, form.password); setMessage({ type: 'success', text: 'Đặt lại mật khẩu thành công. Bạn có thể đăng nhập ngay.' }) }
    catch (error) { setMessage({ type: 'error', text: error.message }) }
    finally { setLoading(false) }
  }
  return <AuthLayout><header className="form-heading"><h1>Đặt lại mật khẩu</h1><p>Tạo mật khẩu mới cho tài khoản của bạn.</p></header><form className="auth-form" onSubmit={submit}><FormField label="Mật khẩu mới" icon={LockIcon} type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} required /><FormField label="Xác nhận mật khẩu" icon={LockIcon} type="password" value={form.confirm} onChange={(event) => setForm({ ...form, confirm: event.target.value })} required />{message && <p className={`form-message ${message.type}`}>{message.text}</p>}<button className="submit-button" disabled={loading}>{loading ? 'Đang cập nhật...' : 'Đặt lại mật khẩu'}</button></form>{message?.type === 'success' && <p className="switch-page"><Link to="/login">Đăng nhập ngay</Link></p>}</AuthLayout>
}
