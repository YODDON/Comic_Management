import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { getProfile } from '../services/profileService'
import { useLanguage } from '../contexts/LanguageContext'

export default function AdminRoute({ children }) {
  const { tr } = useLanguage()
  const hasToken = Boolean(localStorage.getItem('accessToken'))
  const [state, setState] = useState(hasToken ? 'loading' : 'login')

  useEffect(() => {
    if (!hasToken) return
    getProfile().then((result) => setState(result.data.roles?.includes('Admin') ? 'admin' : 'denied')).catch(() => setState('login'))
  }, [hasToken])

  if (state === 'loading') return <div className="admin-route-loading"><span /><p>{tr('Đang kiểm tra quyền quản trị...', 'Checking administrator access...')}</p></div>
  if (state === 'login') return <Navigate to="/login" replace />
  if (state === 'denied') return <Navigate to="/" replace />
  return children
}
