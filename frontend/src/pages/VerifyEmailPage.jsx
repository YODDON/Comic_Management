import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { verifyEmail } from '../services/authService'

export default function VerifyEmailPage() {
  const [params] = useSearchParams()
  const token = params.get('token')
  const started = useRef(false)
  const [status, setStatus] = useState(token ? 'loading' : 'error')
  const [message, setMessage] = useState(token ? 'Đang xác nhận địa chỉ email của bạn...' : 'Liên kết xác nhận thiếu token.')

  useEffect(() => {
    if (started.current) return
    started.current = true
    if (!token) return

    verifyEmail(token).then((result) => {
      setStatus('success')
      setMessage(result.message || 'Email đã được xác nhận thành công.')
    }).catch((error) => {
      setStatus('error')
      setMessage(error.message)
    })
  }, [token])

  return (
    <main className="verification-page">
      <section className="verification-card">
        <img className="verification-logo" src="/images/comico-logo.png" alt="Comico" />
        <div className={`verify-status-icon ${status}`}><span>{status === 'loading' ? '…' : status === 'success' ? '✓' : '!'}</span></div>
        <p className="eyebrow">XÁC THỰC TÀI KHOẢN</p>
        <h1>{status === 'loading' ? 'Đang xác nhận email' : status === 'success' ? 'Xác nhận thành công!' : 'Không thể xác nhận'}</h1>
        <p className="verification-note">{message}</p>
        {status === 'success' && <><p className="verification-note">Tài khoản Comico của bạn đã được kích hoạt. Bây giờ bạn có thể đăng nhập.</p><Link className="verify-primary link-button" to="/login">Đăng nhập ngay</Link></>}
        {status === 'error' && <><Link className="verify-primary link-button" to="/login">Về trang đăng nhập</Link><p className="verification-note">Nếu liên kết đã hết hạn, hãy đăng ký lại hoặc yêu cầu gửi email mới.</p></>}
      </section>
    </main>
  )
}
