import { useState } from 'react'
import { Link } from 'react-router-dom'
import AuthLayout from '../components/AuthLayout'
import FormField from '../components/FormField'
import { MailIcon } from '../components/Icons'
import { forgotPassword } from '../services/authService'

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState(null)
  const [loading, setLoading] = useState(false)
  async function submit(event) {
    event.preventDefault(); setLoading(true); setMessage(null)
    try { await forgotPassword(email); setMessage({ type: 'success', text: 'Nếu email tồn tại, liên kết đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư.' }) }
    catch (error) { setMessage({ type: 'error', text: error.message }) }
    finally { setLoading(false) }
  }
  return <AuthLayout><header className="form-heading"><h1>Quên mật khẩu</h1><p>Nhập email để nhận liên kết đặt lại mật khẩu.</p></header><form className="auth-form" onSubmit={submit}><FormField label="Email" icon={MailIcon} name="email" type="email" placeholder="email@example.com" value={email} onChange={(event) => setEmail(event.target.value)} required />{message && <p className={`form-message ${message.type}`}>{message.text}</p>}<button className="submit-button" disabled={loading}>{loading ? 'Đang gửi...' : 'Gửi liên kết đặt lại'}</button></form><p className="switch-page"><Link to="/login">Quay lại đăng nhập</Link></p></AuthLayout>
}
