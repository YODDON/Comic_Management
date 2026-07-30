const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Authorization', `Bearer ${localStorage.getItem('accessToken') || ''}`)
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const raw = await response.text()
  let result = null
  try { result = raw ? JSON.parse(raw) : null } catch { /* Preserve a short server error below. */ }
  if (response.ok && !result) {
    throw new Error(`API trả về dữ liệu không hợp lệ cho ${path}. Hãy kiểm tra proxy và API Gateway.`)
  }
  if (!response.ok || result?.success === false) {
    const serverMessage = raw && raw.length < 300 && !raw.trimStart().startsWith('<') ? raw : null
    throw new Error(result?.message || result?.title || serverMessage || 'Không thể xử lý yêu cầu quản trị.')
  }
  return result
}

export const getCategories = ({ page = 1, pageSize = 20, search = '' }) => request(`/categories?pageNumber=${page}&pageSize=${pageSize}&search=${encodeURIComponent(search)}`)
export const getCategory = (id) => request(`/categories/${encodeURIComponent(id)}`)
export const createCategory = (data) => request('/categories', { method: 'POST', body: JSON.stringify(data) })
export const updateCategory = (id, data) => request(`/categories/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const deleteCategory = (id) => request(`/categories/${id}`, { method: 'DELETE' })

export const getComics = ({ page = 1, pageSize = 10, search = '', status = '' }) => request(`/comics?pageNumber=${page}&pageSize=${pageSize}&search=${encodeURIComponent(search)}${status ? `&status=${status}` : ''}`)
export const getComicBySlug = (slug) => request(`/comics/slug/${encodeURIComponent(slug)}`)
export const createComic = (data) => request('/comics', { method: 'POST', body: data })
export const updateComic = (id, data) => request(`/comics/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const uploadComicCover = (file) => {
  const body = new FormData()
  body.append('file', file)
  return request('/comics/upload-cover', { method: 'POST', body })
}
export const updateComicStatus = (id, status) => request(`/comics/${id}/status`, { method: 'PUT', body: JSON.stringify({ status: { Ongoing: 0, Completed: 1, Dropped: 2 }[status] ?? status }) })
export const markOutstanding = (comicId) => request('/comics/outstandings', { method: 'POST', body: JSON.stringify({ comicId, priority: 0 }) })
export const toggleOutstanding = (comicId) => request('/comics/outstandings/toggle', { method: 'POST', body: JSON.stringify({ comicId, priority: 0 }) })
export const getChapters = (comicId) => request(`/chapters?comicId=${comicId}&pageSize=100`)
export const getChapter = (id, includePages = false) => request(`/chapters/${id}?includePages=${includePages}`)
export const createChapter = (data) => request('/chapters', { method: 'POST', body: JSON.stringify(data) })
export const updateChapter = (id, data) => request(`/chapters/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const uploadChapterPages = (chapterId, files) => {
  const body = new FormData()
  files.forEach((file) => body.append('files', file))
  return request(`/chapters/${chapterId}/pages`, { method: 'POST', body })
}
export const addChapterPagesByUrls = (chapterId, urls) => request(`/chapters/${chapterId}/pages/urls`, { method: 'POST', body: JSON.stringify({ urls }) })
export const reorderChapterPages = (chapterId, pages) => request(`/chapters/${chapterId}/pages/reorder`, { method: 'PUT', body: JSON.stringify(pages) })
export const deleteChapterPage = (chapterId, pageId) => request(`/chapters/${chapterId}/pages/${pageId}`, { method: 'DELETE' })
export const deleteChapterPages = (chapterId, pageIds) => request(`/chapters/${chapterId}/pages`, { method: 'DELETE', body: JSON.stringify({ pageIds }) })
export const deleteChapter = (id) => request(`/chapters/${id}`, { method: 'DELETE' })

export function adminToast(type, title, text) {
  window.dispatchEvent(new CustomEvent('admin-toast', { detail: { type, title, text } }))
}
