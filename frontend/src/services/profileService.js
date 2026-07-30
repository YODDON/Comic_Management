const API_URL = import.meta.env.VITE_API_URL ?? ''

function getToken() {
  return localStorage.getItem('accessToken')
}

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${getToken()}`)
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const result = await response.json().catch(() => null)
  if (!response.ok || result?.success === false) {
    if (response.status === 401) throw new Error('Phiên đăng nhập đã hết hạn.')
    throw new Error(result?.message || result?.errors?.[0] || 'Không thể xử lý yêu cầu.')
  }
  return result
}

export const getProfile = () => request('/auth/me')
export const updateProfile = (username) => request('/auth/me', {
  method: 'PUT',
  body: JSON.stringify({ username }),
})
export const changePassword = (payload) => request('/auth/me/password', {
  method: 'PUT',
  body: JSON.stringify(payload),
})
export const updateAvatar = (file) => {
  const body = new FormData()
  body.append('avatar', file)
  return request('/auth/me/avatar', { method: 'POST', body })
}
export const logout = () => request('/auth/logout', {
  method: 'POST',
  body: JSON.stringify({ refreshToken: localStorage.getItem('refreshToken') || '' }),
})
