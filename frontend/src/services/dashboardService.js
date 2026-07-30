const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request(path) {
  const response = await fetch(`${API_URL}${path}`, {
    headers: { Authorization: `Bearer ${localStorage.getItem('accessToken') || ''}` },
  })
  const result = await response.json().catch(() => null)
  if (!response.ok || result?.success === false) throw new Error(result?.message || `Không thể tải dữ liệu ${path}.`)
  return result?.data ?? result
}

const totalOf = (data) => Number(data?.totalCount ?? data?.total ?? data?.items?.length ?? (Array.isArray(data) ? data.length : 0))

export async function getDashboardData() {
  const calls = {
    approved: request('/comics?pageNumber=1&pageSize=1&status=Ongoing'),
    pending: request('/comics?pageNumber=1&pageSize=1&status=Completed'),
    rejected: request('/comics?pageNumber=1&pageSize=1&status=Dropped'),
    categories: request('/categories?pageNumber=1&pageSize=1'),
    missions: request('/missions/admin'),
    notifications: request('/notifications/admin'),
    banners: request('/banners/admin'),
    transactions: request('/payments/transactions?pageNumber=1&pageSize=100'),
  }
  const entries = await Promise.all(Object.entries(calls).map(async ([key, promise]) => {
    try { return [key, await promise] } catch { return [key, null] }
  }))
  const data = Object.fromEntries(entries)
  const approved = totalOf(data.approved)
  const pending = totalOf(data.pending)
  const rejected = totalOf(data.rejected)
  const transactions = data.transactions?.items || []
  const transactionAmount = transactions.reduce((sum, item) => sum + Number(item.amount || 0), 0)

  return {
    comics: { total: approved + pending + rejected, approved, pending, rejected },
    system: {
      comics: approved + pending + rejected,
      categories: totalOf(data.categories),
      notifications: totalOf(data.notifications),
      missions: totalOf(data.missions),
      banners: totalOf(data.banners),
    },
    finance: {
      transactionCount: totalOf(data.transactions),
      transactionAmount,
    },
    queue: { pendingComics: pending },
  }
}
