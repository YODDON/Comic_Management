import { Link } from 'react-router-dom'
import BrandPanel from './BrandPanel'
import { ArrowLeftIcon } from './Icons'

export default function AuthLayout({ children }) {
  return (
    <main className="auth-layout">
      <section className="form-panel">
        <Link className="back-button" to="/" aria-label="Quay lại"><ArrowLeftIcon /></Link>
        <div className="form-wrap">{children}</div>
      </section>
      <BrandPanel />
    </main>
  )
}
