export function currentCommentAuthor(anonymous = false) {
  if (anonymous) return 'Ẩn danh'
  const savedUsername = localStorage.getItem('commentUsername')
  if (savedUsername) return savedUsername
  try {
    const token = localStorage.getItem('accessToken') || ''
    const encoded = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const payload = JSON.parse(decodeURIComponent(escape(atob(encoded))))
    return payload.unique_name || payload.name || payload.username || 'Độc giả'
  } catch {
    return 'Độc giả'
  }
}
