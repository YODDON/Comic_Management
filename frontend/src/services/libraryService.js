const keys = { favorite: 'library:favorites', following: 'library:following' }

export function getLibrary(kind) {
  try { return JSON.parse(localStorage.getItem(keys[kind]) || '[]') }
  catch { return [] }
}

export function hasInLibrary(kind, comicId) {
  return getLibrary(kind).some((item) => String(item.id) === String(comicId))
}

export function toggleLibrary(kind, comic, chapterCount = 0) {
  const items = getLibrary(kind)
  const index = items.findIndex((item) => String(item.id) === String(comic.id))
  if (index >= 0) items.splice(index, 1)
  else items.unshift({ id: comic.id, slug: comic.slug, title: comic.title, coverUrl: comic.coverUrl, authorName: comic.authorName, isOutstanding: comic.isOutstanding, chapterCount })
  localStorage.setItem(keys[kind], JSON.stringify(items))
  window.dispatchEvent(new CustomEvent('library-updated'))
  return index < 0
}

export function updateFollowingComic(comic) {
  const items = getLibrary('following')
  const index = items.findIndex((item) => String(item.id) === String(comic.id))
  if (index < 0) return
  items[index] = { ...items[index], ...comic }
  localStorage.setItem(keys.following, JSON.stringify(items))
}
