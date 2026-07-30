const API_URL = import.meta.env.VITE_API_URL ?? ''

async function get(path) {
  const response = await fetch(`${API_URL}${path}`)
  if (!response.ok) throw new Error(`Request failed: ${response.status}`)
  const result = await response.json()
  return result?.data ?? result
}

export const getBanners = () => get('/banners')
export const getHotComics = () => get('/comics/hot?limit=8')
