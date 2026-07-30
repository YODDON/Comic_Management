const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  const token = localStorage.getItem('accessToken')
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  const result = await response.json().catch(() => null)
  if (!response.ok || result?.success === false) {
    const error = new Error(result?.message || 'Không thể tải dữ liệu truyện.')
    error.status = response.status
    error.data = result?.data
    throw error
  }
  if (!response.ok || result?.success === false) throw new Error(result?.message || 'Không thể tải dữ liệu truyện.')
  return result?.data ?? result
}

export const getNewComics = (limit = 8) => request(`/comics?pageNumber=1&pageSize=${limit}&status=Ongoing`)
export const getOutstandingComics = (limit = 8) => request(`/comics/outstandings?limit=${limit}`)
export const getOutstandingComicsPaged = ({ page = 1, pageSize = 20 } = {}) => request(`/comics/outstandings/paged?pageNumber=${page}&pageSize=${pageSize}`)
export const getRankingComics = () => request('/comics/hot?limit=10')
export const getPublicComics = ({ page = 1, pageSize = 20 } = {}) => request(`/comics?pageNumber=${page}&pageSize=${pageSize}&status=Ongoing`)
export const searchComics = ({ search = '', categoryIds = [], page = 1, pageSize = 16 } = {}) => request(`/comics?pageNumber=${page}&pageSize=${pageSize}&status=Ongoing&search=${encodeURIComponent(search)}${categoryIds.map((id) => `&categoryId=${encodeURIComponent(id)}`).join('')}`)
export const getCategories = () => request('/categories?pageNumber=1&pageSize=100')
export const getComic = (slug) => request(`/comics/slug/${encodeURIComponent(slug)}`)
export const getComicById = (id) => request(`/comics/${encodeURIComponent(id)}`)
export const getComicChapters = (comicId) => request(`/chapters?comicId=${comicId}&status=Published&pageNumber=1&pageSize=200`)
export const getChapterPages = (comicId, slug) => request(`/chapters/slug/${encodeURIComponent(slug)}/pages?comicId=${comicId}`)
export const purchaseChapter = (chapterId) => request('/payments/purchased-chapter', {
  method: 'POST',
  body: JSON.stringify({ chapterId }),
})
export const getComments = (comicId) => request(`/comments?comicId=${comicId}&page=1&pageSize=50`)
export const postComment = (comicId, content, parentCommentId = null) => request('/comments', { method: 'POST', body: JSON.stringify({ comicId, content, parentCommentId }) })
