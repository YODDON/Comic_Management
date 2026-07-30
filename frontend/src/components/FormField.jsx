import { useState } from 'react'
import { EyeIcon } from './Icons'

export default function FormField({ label, icon: Icon, type = 'text', ...props }) {
  const [visible, setVisible] = useState(false)
  const isPassword = type === 'password'

  return (
    <label className="field">
      <span className="field-label"><Icon />{label}</span>
      <span className="input-shell">
        <input type={isPassword && visible ? 'text' : type} {...props} />
        {isPassword && (
          <button type="button" className="eye-button" onClick={() => setVisible((value) => !value)} aria-label={visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}>
            <EyeIcon hidden={!visible} />
          </button>
        )}
      </span>
    </label>
  )
}
