import { Link } from 'react-router-dom'

export default function ComicCard({ comic, title, author, featuredLabel = 'Nổi bật' }) {
  const displayTitle = title || comic.title
  return <Link className="public-comic-card" to={`/comics/${comic.slug}`}>
    <div className="public-comic-cover" style={comic.coverUrl ? { backgroundImage: `url("${comic.coverUrl}")` } : undefined}>
      {!comic.coverUrl && <b>{displayTitle?.[0]}</b>}
      {comic.isOutstanding && <span>★ {featuredLabel}</span>}
    </div>
    <h3>{displayTitle}</h3><p>{author || comic.authorName || 'Comico'}</p>
  </Link>
}
