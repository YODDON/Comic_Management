const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${localStorage.getItem('accessToken') || ''}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const contentType = response.headers.get('content-type') || ''
  const result = contentType.includes('application/json') ? await response.json().catch(() => null) : null
  if (!response.ok || result?.success === false) {
    if (response.status === 401) throw new Error('Vui lòng đăng nhập để xem nhiệm vụ.')
    throw new Error(result?.message || 'Không thể xử lý yêu cầu nhiệm vụ.')
  }
  if (!result) throw new Error('API nhiệm vụ không trả về dữ liệu JSON. Hãy kiểm tra API Gateway và khởi động lại Vite.')
  return result
}

export const getMyMissions = () => request('/missions/my-missions')
export const completeMission = (id) => request(`/missions/${id}/complete`, { method: 'POST' })
export const getAdminMissions = () => request('/missions/admin')
export const createMission = (data) => request('/missions', { method: 'POST', body: JSON.stringify(data) })
export const updateMission = (id, data) => request(`/missions/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const deleteMission = (id) => request(`/missions/${id}`, { method: 'DELETE' })
export const trackLobbyMinute = () => request('/missions/lobby-heartbeat', { method: 'POST' })
