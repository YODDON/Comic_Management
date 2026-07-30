/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useMemo, useState } from 'react'

const STORAGE_KEY = 'siteLanguage'
const LanguageContext = createContext(null)

export const SITE_LANGUAGES = [
  { value: 'vi', shortLabel: 'VI', label: 'Tiếng Việt' },
  { value: 'en', shortLabel: 'EN', label: 'English' },
]

function initialLanguage() {
  const saved = localStorage.getItem(STORAGE_KEY)
    || localStorage.getItem('comicTranslationLanguage')
  return saved === 'en' ? 'en' : 'vi'
}

export function LanguageProvider({ children }) {
  const [language, setLanguageState] = useState(initialLanguage)

  const value = useMemo(() => ({
    language,
    locale: language === 'en' ? 'en-US' : 'vi-VN',
    setLanguage(nextLanguage) {
      const normalized = nextLanguage === 'en' ? 'en' : 'vi'
      localStorage.setItem(STORAGE_KEY, normalized)
      localStorage.setItem('comicTranslationLanguage', normalized)
      document.documentElement.lang = normalized
      setLanguageState(normalized)
    },
    tr(vietnamese, english) {
      return language === 'en' ? english : vietnamese
    },
  }), [language])

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage() {
  const context = useContext(LanguageContext)
  if (!context) throw new Error('useLanguage must be used inside LanguageProvider.')
  return context
}
