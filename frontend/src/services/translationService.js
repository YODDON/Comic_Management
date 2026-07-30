const API_URL = import.meta.env.VITE_API_URL ?? ''

export async function translateTexts(targetLanguage, texts) {
  const cleanTexts = texts
    .filter((item) => item?.key && item?.value?.trim())
    .map((item) => ({ key: item.key, value: item.value.trim() }))
  if (!cleanTexts.length) return {}

  const response = await fetch(`${API_URL}/translations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      targetLanguage,
      texts: cleanTexts,
    }),
  })
  const result = await response.json().catch(() => null)
  if (!response.ok || result?.success === false) {
    throw new Error(result?.message || 'Không thể dịch nội dung lúc này.')
  }

  const translatedItems = result?.data?.items || []
  return Object.fromEntries(translatedItems.map((item) => [item.key, item.value]))
}
