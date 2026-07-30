const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  const token = localStorage.getItem('accessToken')
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const result = await response.json().catch(() => null)
  const validationMessage = result?.errors
    ? Object.values(result.errors).flat().find(Boolean)
    : null
  if (!response.ok || result?.success === false) throw new Error(validationMessage || result?.message || result?.title || 'Không thể xử lý banner.')
  return result?.data ?? result
}

export const getAdminBanners = () => request('/banners/admin')
export const getAdminBanner = (id) => request(`/banners/${id}`)
export const createBanner = (payload) => request('/banners', { method: 'POST', body: JSON.stringify(payload) })
export const updateBanner = (id, payload) => request(`/banners/${id}`, { method: 'PUT', body: JSON.stringify(payload) })
export const deleteBanner = (id) => request(`/banners/${id}`, { method: 'DELETE' })
export const uploadBannerImage = (file) => {
  const body = new FormData()
  body.append('file', file)
  return request('/banners/upload', { method: 'POST', body })
}
