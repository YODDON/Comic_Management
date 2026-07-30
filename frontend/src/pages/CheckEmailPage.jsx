import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { resendVerification } from '../services/authService'

export default function CheckEmailPage() {
  const [params] = useSearchParams()
  const email = params.get('email') || ''
  const [message, setMessage] = useState(null)
  const [loading, setLoading] = useState(false)

  async function resend() {
    if (!email) return
    setLoading(true)
    setMessage(null)
    try {
      await resendVerification(email)
      setMessage({ type: 'success', text: 'Email xác nhận mới đã được gửi.' })
    } catch (error) {
      setMessage({ type: 'error', text: error.message })
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="verification-page">
      <section className="verification-card">
        <img className="verification-logo" src="/images/comico-logo.png" alt="Comico" />
        <div className="mail-illustration">✉<i>✓</i></div>
        <p className="eyebrow">ĐĂNG KÝ THÀNH CÔNG</p>
        <h1>Kiểm tra hộp thư của bạn</h1>
        <p>Comico đã gửi liên kết xác nhận đến</p>
        <strong className="email-address">{email || 'email của bạn'}</strong>
        <p className="verification-note">Vui lòng bấm nút <b>“Xác nhận email”</b> trong thư. Bạn chỉ có thể đăng nhập sau khi hoàn tất bước này.</p>
        {message && <div className={`verify-message ${message.type}`}>{message.text}</div>}
        <button className="verify-primary" onClick={resend} disabled={!email || loading}>{loading ? 'Đang gửi...' : 'Gửi lại email xác nhận'}</button>
        <Link className="verify-secondary" to="/login">Quay lại đăng nhập</Link>
        <small>Không thấy email? Hãy kiểm tra thư mục Spam hoặc Quảng cáo.</small>
      </section>
    </main>
  )
}
