const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${localStorage.getItem('accessToken') || ''}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const contentType = response.headers.get('content-type') || ''
  const result = contentType.includes('application/json') ? await response.json().catch(() => null) : null
  if (!response.ok || result?.success === false) {
    if (response.status === 401) throw new Error('Vui lòng đăng nhập để xem ví của bạn.')
    throw new Error(result?.message || 'Không thể xử lý yêu cầu ví.')
  }
  if (!result) throw new Error('API ví không trả về dữ liệu JSON. Hãy kiểm tra API Gateway (cổng 5028) và khởi động lại Vite.')
  return result
}

// { balance, withdrawable, locked } — balance includes non-withdrawable mission coins (F912).
export const getWalletSummary = () => request('/withdraws/withdrawable')

// Deposit (F904): create a pending top-up + VietQR image.
// -> { transactionId, transactionCode, amount, qrUrl }
export const createDeposit = (amount) =>
  request('/payments/deposit', { method: 'POST', body: JSON.stringify({ amount }) })

// Poll a transaction's status. PaymentAPI serialises the enum as an int:
// 0 Pending, 1 Completed, 2 Rejected, 3 Failed.
export const checkTransaction = (id) => request(`/payments/transactions/check/${id}`)

export const TX_STATUS = { PENDING: 0, COMPLETED: 1, REJECTED: 2, FAILED: 3 }

// Transaction history (F915): the current user's deposits + purchases.
// PagedResult<TransactionDto>; type and status are ints (PaymentAPI serialises enums as int).
export const getTransactions = (page = 1, size = 10) =>
  request(`/payments/transactions?pageNumber=${page}&pageSize=${size}`)

export const TX_TYPE = { PURCHASE: 0, MANUAL_TOPUP: 1, WITHDRAW: 2, REFUND: 3, MISSION_REWARD: 4, FEE: 5 }

// --- Admin: withdraw approval (F907a) ---
// WalletAPI serialises WithdrawStatus as a STRING ("Pending" / "Approved" / "Rejected").
export const WITHDRAW_STATUS = { PENDING: 'Pending', APPROVED: 'Approved', REJECTED: 'Rejected' }

// PagedResult<WithdrawDto>. Pass status = '' for all statuses.
export const getAdminWithdraws = (status, page = 1, size = 10, search = '') => {
  const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(size) })
  if (status) params.set('status', status)
  if (search.trim()) params.set('search', search.trim())
  return request(`/withdraws/admin?${params.toString()}`)
}

// status must be 'Approved' or 'Rejected'. A 409 means it was already processed by someone else.
export const updateWithdrawStatus = (id, status, note) =>
  request(`/withdraws/${id}`, { method: 'PUT', body: JSON.stringify({ status, note: note || null }) })

// --- Admin: deposit monitoring (F907b, view-only) ---
// type=ManualTopUp only. Status/type come back as ints (see TX_STATUS/TX_TYPE).
export const getAdminDeposits = (page = 1, size = 10, status = '', search = '') => {
  const params = new URLSearchParams({
    type: 'ManualTopUp',
    pageNumber: String(page),
    pageSize: String(size),
  })
  if (status !== '') params.set('status', String(status))
  if (search.trim()) params.set('search', search.trim())
  return request(`/payments/transactions?${params.toString()}`)
}
