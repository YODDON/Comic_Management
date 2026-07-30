import { useState } from 'react'

export default function CommentThread({
  comment,
  prefix = '',
  onReply,
  canReply = false,
}) {
  const [open, setOpen] = useState(false)
  const [text, setText] = useState('')
  const [anonymous, setAnonymous] = useState(false)
  const raw = prefix && comment.content?.startsWith(prefix)
    ? comment.content.slice(prefix.length)
    : comment.content
  const match = raw?.match(/^\[author:([^\]]+)\]/)
  const author = match?.[1] || 'Độc giả'
  const content = match ? raw.slice(match[0].length) : raw

  async function submit(event) {
    event.preventDefault()
    await onReply(text, comment.id, anonymous)
    setText('')
    setOpen(false)
  }

  return (
    <article className="comment-thread">
      <b>{author}</b>
      <p>{content}</p>
      <time>{new Date(comment.createdAt).toLocaleString('vi-VN')}</time>
      {canReply && (
        <button className="reply-toggle" onClick={() => setOpen((value) => !value)}>
          ↩ Trả lời
        </button>
      )}
      {canReply && open && (
        <form className="reply-form" onSubmit={submit}>
          <input
            required
            value={text}
            onChange={(event) => setText(event.target.value)}
            placeholder="Viết câu trả lời..."
          />
          <button>Gửi</button>
          <label>
            <input
              type="checkbox"
              checked={anonymous}
              onChange={(event) => setAnonymous(event.target.checked)}
            />
            Ẩn danh
          </label>
        </form>
      )}
      {comment.replies?.length > 0 && (
        <div className="comment-replies">
          {comment.replies.map((reply) => (
            <CommentThread
              key={reply.id}
              comment={reply}
              prefix={prefix}
              onReply={onReply}
              canReply={canReply}
            />
          ))}
        </div>
      )}
    </article>
  )
}
