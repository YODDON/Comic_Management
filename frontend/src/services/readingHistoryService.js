const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path, options = {}) {
  const token = localStorage.getItem('accessToken')
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, ...options.headers },
  })
  const result = await response.json().catch(() => null)
  if (!response.ok) throw new Error(result?.message || 'Không thể lưu hoặc tải lịch sử đọc.')
  return result?.data ?? result
}

export const getReadingHistory = () => request('/reading-history/me?page=1&pageSize=100')

export function getReadingMetadata(comicId) {
  try { return JSON.parse(localStorage.getItem(`reading-history:${comicId}`) || 'null') }
  catch { return null }
}

export function getLocalReadingHistory() {
  const items = []
  for (let index = 0; index < localStorage.length; index += 1) {
    const key = localStorage.key(index)
    if (!key?.startsWith('reading-history:')) continue
    try {
      const value = JSON.parse(localStorage.getItem(key))
      if (value?.comicId && value?.chapterId) items.push({ id: `local-${value.comicId}`, ...value })
    } catch { /* Ignore invalid data left by an older frontend version. */ }
  }
  return items.sort((a, b) => new Date(b.readAt) - new Date(a.readAt))
}

export function recordReadingHistory(comicId, chapterId, metadata = {}) {
  localStorage.setItem(`reading-history:${comicId}`, JSON.stringify({ ...metadata, comicId, chapterId, readAt: new Date().toISOString() }))
  return request('/reading-history', { method: 'POST', body: JSON.stringify({ comicId, chapterId }) })
}
