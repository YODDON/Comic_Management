import { useEffect, useMemo, useState } from 'react'
import { useLanguage } from '../contexts/LanguageContext'
import { translateTexts } from '../services/translationService'

const translationCache = new Map()

function normalizeTexts(texts) {
  const unique = new Map()
  for (const item of texts || []) {
    const key = String(item?.key || '').trim()
    const value = String(item?.value || '').trim()
    if (key && value) unique.set(key, { key, value })
  }
  return [...unique.values()]
}

export default function useTranslatedTexts(texts) {
  const { language } = useLanguage()
  const signature = JSON.stringify(normalizeTexts(texts))
  const normalized = useMemo(() => JSON.parse(signature), [signature])
  const [translations, setTranslations] = useState({})
  const [translating, setTranslating] = useState(false)
  const [translationError, setTranslationError] = useState('')

  useEffect(() => {
    let cancelled = false
    const task = window.setTimeout(() => {
      if (!normalized.length) {
        setTranslations({})
        setTranslationError('')
        setTranslating(false)
        return
      }

      if (language === 'vi') {
        setTranslations(Object.fromEntries(normalized.map((item) => [item.key, item.value])))
        setTranslationError('')
        setTranslating(false)
        return
      }

      const cached = {}
      const missing = []
      for (const item of normalized) {
        const cacheKey = `${language}:${item.value}`
        if (translationCache.has(cacheKey)) cached[item.key] = translationCache.get(cacheKey)
        else missing.push(item)
      }

      setTranslations(cached)
      setTranslationError('')
      if (!missing.length) {
        setTranslating(false)
        return
      }

      setTranslating(true)
      translateTexts(language, missing)
        .then((result) => {
          if (cancelled) return
          const next = { ...cached, ...result }
          for (const item of missing) {
            if (next[item.key]) translationCache.set(`${language}:${item.value}`, next[item.key])
          }
          setTranslations(next)
        })
        .catch((error) => {
          if (cancelled) return
          setTranslationError(error.message)
        })
        .finally(() => {
          if (!cancelled) setTranslating(false)
        })
    }, 0)

    return () => {
      cancelled = true
      window.clearTimeout(task)
    }
  }, [language, normalized])

  return { translations, translating, translationError }
}
