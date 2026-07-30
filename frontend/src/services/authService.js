const API_URL = import.meta.env.VITE_API_URL ?? ''

const wait = (milliseconds) => new Promise((resolve) => setTimeout(resolve, milliseconds))

async function requestWithRetry(path, options) {
  let lastError
  for (let attempt = 0; attempt < 5; attempt += 1) {
    try {
      return await fetch(`${API_URL}${path}`, options)
    } catch (error) {
      lastError = error
      if (attempt < 4) await wait(700)
    }
  }
  throw lastError
}

async function send(path, body) {
  let response
  try {
    response = await requestWithRetry(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
  } catch {
    throw new Error('Không thể kết nối API Gateway. Hãy chạy Gateway tại cổng 5028.')
  }

  const result = await response.json().catch(() => null)
  if (!response.ok || result?.success === false) {
    const message = result?.message || result?.errors?.[0] || 'Máy chủ phản hồi không hợp lệ.'
    throw new Error(message)
  }

  return result
}

export async function verifyEmail(token) {
  let response
  try {
    response = await requestWithRetry(`/auth/verify-email?token=${encodeURIComponent(token)}`, { method: 'GET' })
  } catch {
    throw new Error('Không thể kết nối API Gateway. Vui lòng thử lại.')
  }

  const result = await response.json().catch(() => null)
  if (!response.ok || result?.success === false) {
    throw new Error(result?.message || 'Liên kết xác nhận không hợp lệ hoặc đã hết hạn.')
  }
  return result
}

export const login = (credentials) => send('/auth/login', credentials)
export const register = (account) => send('/auth/register', account)
export const resendVerification = (email) => send('/auth/resend-confirm', { email })
export const forgotPassword = (email) => send('/auth/forgot-password', { email })
export const resetPassword = (token, newPassword) => send('/auth/reset-password', { token, newPassword })

export function getRolesInToken(token = localStorage.getItem('accessToken')) {
  if (!token) return []
  try {
    const encodedPayload = token.split('.')[1]
    const normalized = encodedPayload.replace(/-/g, '+').replace(/_/g, '/')
    const payload = JSON.parse(atob(normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=')))
    if (payload.exp && payload.exp * 1000 <= Date.now()) return []

    const claim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      ?? payload.role
      ?? payload.roles
    return (Array.isArray(claim) ? claim : [claim]).filter(Boolean)
  } catch {
    return []
  }
}

export const hasRoleInToken = (role, token) => getRolesInToken(token).includes(role)
export const hasAdminRoleInToken = (token) => hasRoleInToken('Admin', token)
export const hasGuestRoleInToken = (token) => hasRoleInToken('Guest', token)
