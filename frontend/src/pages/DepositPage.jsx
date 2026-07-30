import { useEffect, useRef, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import { createDeposit, checkTransaction, TX_STATUS } from '../services/walletService'

const PRESETS = [10000, 20000, 50000, 100000, 200000, 500000]
const POLL_MS = 3000
const TIMEOUT_MS = 15 * 60 * 1000

const formatCoin = (value) => Number(value || 0).toLocaleString('vi-VN')

// The QR image URL embeds the receiving account; pull it out for the manual-transfer fallback.
function parseBank(qrUrl) {
  const match = /image\/([^-]+)-([^-]+)-/.exec(qrUrl || '')
  return match ? { bankCode: match[1], account: match[2] } : { bankCode: '', account: '' }
}

export default function DepositPage() {
  const navigate = useNavigate()
  const hasToken = Boolean(localStorage.getItem('accessToken'))

  const [step, setStep] = useState('select') // select | qr | success | failed | timeout
  const [amount, setAmount] = useState(10000)
  const [custom, setCustom] = useState('10000')
  const [deposit, setDeposit] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [copied, setCopied] = useState(false)
  const timers = useRef([])

  const clearTimers = () => { timers.current.forEach(clearInterval); timers.current.forEach(clearTimeout); timers.current = [] }
  useEffect(() => () => clearTimers(), [])

  if (!hasToken) return <Navigate to="/login" replace />

  function pickPreset(value) { setAmount(value); setCustom(String(value)) }
  function onCustom(e) {
    const raw = e.target.value.replace(/\D/g, '')
    setCustom(raw)
    setAmount(Number(raw || 0))
  }

  async function submit() {
    if (amount <= 0) { setError('Vui lòng chọn số Dâu muốn nạp.'); return }
    setError(null)
    setSubmitting(true)
    try {
      const result = await createDeposit(amount)
      setDeposit(result.data)
      setStep('qr')
      startPolling(result.data.transactionId)
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  function startPolling(transactionId) {
    clearTimers()
    const poll = window.setInterval(async () => {
      try {
        const result = await checkTransaction(transactionId)
        const status = result.data?.status
        if (status === TX_STATUS.COMPLETED) { clearTimers(); setStep('success') }
        else if (status === TX_STATUS.REJECTED || status === TX_STATUS.FAILED) { clearTimers(); setStep('failed') }
      } catch { /* keep polling; a transient error shouldn't abort the wait */ }
    }, POLL_MS)
    const timeout = window.setTimeout(() => { clearTimers(); setStep('timeout') }, TIMEOUT_MS)
    timers.current = [poll, timeout]
  }

  function reset() {
    clearTimers()
    setDeposit(null)
    setStep('select')
    setCopied(false)
  }

  function copyCode() {
    if (!deposit) return
    navigator.clipboard?.writeText(deposit.transactionCode)
    setCopied(true)
    window.setTimeout(() => setCopied(false), 1500)
  }

  return (
    <div className="deposit-page">
      <SiteHeader />
      <main>
        <div className="wallet-heading">
          <p><Link to="/wallet">← Ví của tôi</Link></p>
          <h1>🍓 Nạp Dâu Tây</h1>
          <span>Nạp tiền để mua chương truyện trả phí (1 🍓 = 1 VND).</span>
        </div>

        {error && <div className="wallet-notice error"><b>!</b><span>{error}</span></div>}

        {step === 'select' && (
          <section className="deposit-card">
            <div className="deposit-guide">
              <h3>Hướng dẫn nạp tiền</h3>
              <ol>
                <li>Chọn số lượng Dâu muốn nạp.</li>
                <li>Bấm <b>"Gửi yêu cầu nạp tiền"</b> — hệ thống tạo mã QR riêng cho bạn.</li>
                <li>Quét QR bằng app ngân hàng, chuyển <b>đúng số tiền</b> và <b>đúng nội dung</b>.</li>
                <li>Dâu vào ví <b>tự động</b> ngay khi ngân hàng xác nhận.</li>
              </ol>
            </div>
            <label className="deposit-label">Chọn số lượng</label>
            <div className="deposit-presets">
              {PRESETS.map((value) => (
                <button key={value} className={amount === value ? 'active' : ''} onClick={() => pickPreset(value)}>
                  {formatCoin(value)} 🍓
                </button>
              ))}
            </div>
            <div className="deposit-custom">
              <input inputMode="numeric" value={custom} onChange={onCustom} placeholder="Nhập số Dâu" />
              <span>🍓</span>
            </div>
            <div className="deposit-summary">
              <span>Số lượng nạp</span><b>{formatCoin(amount)} 🍓</b>
            </div>
            <div className="deposit-summary muted">
              <span>Tương đương</span><b>{formatCoin(amount)} VND (1 🍓 = 1 VND)</b>
            </div>
            <button className="deposit-submit" disabled={submitting || amount <= 0} onClick={submit}>
              {submitting ? 'Đang tạo mã…' : '➤ Gửi yêu cầu nạp tiền'}
            </button>
          </section>
        )}

        {step === 'qr' && deposit && (
          <section className="deposit-card deposit-qr">
            <div className="deposit-qr-head">
              <h3>🏦 Chuyển khoản ngân hàng</h3>
              <span>Chuyển đúng nội dung để được xác nhận tự động.</span>
            </div>
            <img className="deposit-qr-img" src={deposit.qrUrl} alt="VietQR" />
            <p className="deposit-scan">Quét mã QR bằng app ngân hàng để chuyển khoản nhanh.</p>

            <div className="deposit-bank">
              <div><small>Số tiền</small><b>{formatCoin(deposit.amount)} VND</b></div>
              <div><small>Ngân hàng</small><b>{parseBank(deposit.qrUrl).bankCode}</b></div>
              <div><small>Số tài khoản</small><b>{parseBank(deposit.qrUrl).account}</b></div>
              <div className="deposit-code">
                <small>Nội dung chuyển khoản</small>
                <b>{deposit.transactionCode}</b>
                <button onClick={copyCode}>{copied ? '✓ Đã copy' : 'Copy'}</button>
              </div>
            </div>

            <div className="wallet-notice warn">
              <b>!</b>
              <span>Nội dung chuyển khoản <b>phải giữ nguyên</b>. Sai nội dung sẽ không được xác nhận tự động.</span>
            </div>

            <div className="deposit-waiting"><i className="spinner" /> Đang chờ nhận tiền… (tự động cập nhật, tối đa 15 phút)</div>
            <button className="deposit-cancel" onClick={reset}>Huỷ / tạo lại mã khác</button>
          </section>
        )}

        {step === 'success' && deposit && (
          <section className="deposit-card deposit-result success">
            <div className="deposit-result-icon">✓</div>
            <h2>Nạp thành công!</h2>
            <p>Đã cộng <b>{formatCoin(deposit.amount)} 🍓</b> vào ví của bạn.</p>
            <div className="deposit-receipt">
              <div><span>Số tiền</span><b>{formatCoin(deposit.amount)} 🍓</b></div>
              <div><span>Mã giao dịch</span><b>{deposit.transactionId}</b></div>
              <div><span>Thời gian</span><b>{new Date().toLocaleString('vi-VN')}</b></div>
              <div><span>Trạng thái</span><b className="ok">Thành công</b></div>
            </div>
            <div className="deposit-actions">
              <button onClick={reset}>Nạp tiếp</button>
              <button onClick={() => navigate('/wallet/transactions')}>Xem lịch sử giao dịch</button>
              <button onClick={() => navigate('/')}>Về trang chủ</button>
            </div>
          </section>
        )}

        {(step === 'failed' || step === 'timeout') && (
          <section className="deposit-card deposit-result failed">
            <div className="deposit-result-icon">!</div>
            <h2>{step === 'timeout' ? 'Chưa nhận được tiền' : 'Giao dịch không thành công'}</h2>
            <p>
              {step === 'timeout'
                ? 'Hết thời gian chờ. Nếu bạn đã chuyển khoản, tiền sẽ được cộng khi ngân hàng xác nhận — hãy kiểm tra lịch sử hoặc liên hệ admin.'
                : 'Giao dịch bị từ chối (ví dụ chuyển sai/thiếu số tiền). Vui lòng thử lại.'}
            </p>
            <div className="deposit-actions">
              <button onClick={reset}>Thử lại</button>
              <button onClick={() => navigate('/wallet/transactions')}>Xem lịch sử giao dịch</button>
              <button onClick={() => navigate('/wallet')}>Về ví</button>
            </div>
          </section>
        )}
      </main>
    </div>
  )
}
