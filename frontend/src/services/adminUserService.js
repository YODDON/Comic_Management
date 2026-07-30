const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${localStorage.getItem('accessToken') || ''}`)
  if (options.body) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const result = await response.json().catch(() => null)

  if (!response.ok || result?.success === false) {
    throw new Error(result?.message || 'Không thể xử lý dữ liệu người dùng.')
  }

  return result?.data ?? result
}

export const getAdminUsers = ({
  pageNumber = 1,
  pageSize = 20,
  search = '',
  status = '',
} = {}) =>
  request(
    `/auth/admin/users?pageNumber=${pageNumber}&pageSize=${pageSize}&search=${encodeURIComponent(search)}${status === '' ? '' : `&isActive=${status}`}`,
  )

export const getAdminUser = (id) =>
  request(`/auth/admin/users/${id}`)

export const updateAdminUserLock = (id, isLocked) =>
  request(`/auth/admin/users/${id}/lock`, {
    method: 'PUT',
    body: JSON.stringify({ isLocked }),
  })

export const updateAdminUserRole = (id, role) =>
  request(`/auth/admin/users/${id}/role`, {
    method: 'PUT',
    body: JSON.stringify({ role }),
  })
