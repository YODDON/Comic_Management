const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${localStorage.getItem('accessToken') || ''}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const raw = await response.text()
  let result = null
  try { result = raw ? JSON.parse(raw) : null } catch { /* Use the server text below when it is short. */ }
  if (!response.ok || result?.success === false) {
    if (response.status === 401) throw new Error('Vui lòng đăng nhập để xem thông báo.')
    const serverMessage = raw && raw.length < 500 && !raw.trimStart().startsWith('<') ? raw : null
    throw new Error(result?.message || result?.title || serverMessage || 'Không thể xử lý yêu cầu thông báo.')
  }
  if (!result) throw new Error('API thông báo không trả về dữ liệu hợp lệ.')
  return result
}

export const getNotifications = ({ page = 1, pageSize = 50 } = {}) => request(`/notifications?pageIndex=${page}&pageSize=${pageSize}`)
export const createBroadcastNotification = ({ title, body }) => request('/notifications', { method: 'POST', body: JSON.stringify({ userId: null, title, body }) })
export const getAdminNotifications = () => request('/notifications/admin')
export const updateAdminNotification = (id, data) => request(`/notifications/admin/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const deleteAdminNotification = (id) => request(`/notifications/admin/${id}`, { method: 'DELETE' })
