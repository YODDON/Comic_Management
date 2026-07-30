import { useEffect, useMemo, useState } from 'react'
import { Navigate } from 'react-router-dom'
import SiteHeader from '../components/SiteHeader'
import Pagination from '../components/Pagination'
import { completeMission, getMyMissions } from '../services/missionService'
import { useLanguage } from '../contexts/LanguageContext'
import useTranslatedTexts from '../hooks/useTranslatedTexts'

const typeLabels = [
  ['Đọc chương truyện', 'Read chapters'],
  ['Mua chương truyện', 'Unlock chapters'],
  ['Bình luận', 'Comment'],
  ['Treo ở sảnh', 'Lobby time'],
]

const PAGE_SIZE = 6

export default function MissionsPage() {
  const { language, tr } = useLanguage()
  const [missions, setMissions] = useState([])
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [working, setWorking] = useState(null)
  const [notice, setNotice] = useState(null)
  const hasToken = Boolean(localStorage.getItem('accessToken'))
  const totalPages = Math.max(1, Math.ceil(missions.length / PAGE_SIZE))
  const safePage = Math.min(page, totalPages)
  const pagedMissions = useMemo(
    () => missions.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [missions, safePage],
  )
  const translationTexts = useMemo(() => pagedMissions.flatMap((mission) => [
    { key: `mission.${mission.missionId}.title`, value: mission.title },
    { key: `mission.${mission.missionId}.description`, value: mission.description },
  ]), [pagedMissions])
  const { translations, translating, translationError } = useTranslatedTexts(translationTexts)

  useEffect(() => {
    if (!hasToken) return
    const load = (showError = true) => getMyMissions().then((result) => setMissions(result.data || []))
      .catch((error) => { if (showError) setNotice({ type: 'error', text: language === 'en' ? 'Unable to load missions.' : error.message }) }).finally(() => setLoading(false))
    load()
    const timer = window.setInterval(() => load(false), 15000)
    return () => window.clearInterval(timer)
  }, [hasToken, language])

  if (!hasToken) return <Navigate to="/login" replace />

  async function complete(mission) {
    if (mission.currentProgress < mission.targetCount) {
      setNotice({ type: 'error', text: tr('Bạn chưa hoàn thành nhiệm vụ.', 'You have not completed this mission yet.') })
      return
    }
    setWorking(mission.missionId)
    try {
      const result = await completeMission(mission.missionId)
      const updated = result.data
      setMissions((items) => updated.isCompleted
        ? items.filter((item) => item.missionId !== updated.missionId)
        : items.map((item) => item.missionId === updated.missionId ? updated : item))
      const translatedTitle = translations[`mission.${updated.missionId}.title`] || updated.title
      setNotice(updated.isCompleted
        ? { type: 'success', text: tr(`Hoàn thành nhiệm vụ “${translatedTitle}”! Bạn đã nhận ${updated.rewardCoin} Dâu.`, `Mission “${translatedTitle}” completed! You received ${updated.rewardCoin} Dâu.`) }
        : { type: 'success', text: tr(`Đã cập nhật tiến độ: ${updated.currentProgress}/${updated.targetCount}.`, `Progress updated: ${updated.currentProgress}/${updated.targetCount}.`) })
    } catch (error) { setNotice({ type: 'error', text: language === 'en' ? 'Unable to claim the mission reward.' : error.message }) }
    finally { setWorking(null) }
  }

  return <div className="missions-page"><SiteHeader /><main>
    {translationError && <div className="global-translation-alert">{translationError}</div>}
    <div className="mission-title"><p>{tr('TÀI KHOẢN CỦA TÔI', 'MY ACCOUNT')}</p><h1>{tr('Nhiệm vụ', 'Missions')}</h1><span>{tr('Hoàn thành nhiệm vụ để nhận Dâu vào ví của bạn.', 'Complete missions to receive Dâu in your wallet.')}</span></div>
    {notice && <div className={`mission-notice ${notice.type}`}><b>{notice.type === 'success' ? '✓' : '!'}</b><span>{notice.text}</span><button onClick={() => setNotice(null)}>×</button></div>}
    {loading && <div className="mission-empty">{tr('Đang tải nhiệm vụ...', 'Loading missions...')}</div>}
    {!loading && missions.length === 0 && <div className="mission-empty">{tr('Hiện chưa có nhiệm vụ nào.', 'There are no missions available.')}</div>}
    <div className="mission-grid">{pagedMissions.map((mission) => {
      const percent = Math.min(100, mission.targetCount ? mission.currentProgress / mission.targetCount * 100 : 0)
      return <article className={`mission-card ${mission.isCompleted ? 'completed' : ''}`} key={mission.missionId}>
        <div className="mission-card-top"><span>{mission.isCompleted ? '✓' : '★'}</span><div><small>{typeLabels[mission.type] ? tr(...typeLabels[mission.type]) : tr('Nhiệm vụ', 'Mission')}</small><h2>{translations[`mission.${mission.missionId}.title`] || mission.title}</h2></div><strong>+{mission.rewardCoin} Dâu</strong></div>
        <p>{translations[`mission.${mission.missionId}.description`] || mission.description}</p>
        <div className="mission-progress-label"><span>{tr('Tiến độ', 'Progress')}</span><b>{mission.currentProgress}/{mission.targetCount}</b></div>
        <div className="mission-progress"><i style={{ width: `${percent}%` }} /></div>
        <button disabled={mission.isCompleted || working === mission.missionId} onClick={() => complete(mission)}>{mission.isCompleted ? tr('Đã nhận thưởng', 'Reward claimed') : working === mission.missionId ? tr('Đang nhận thưởng...', 'Claiming reward...') : mission.currentProgress >= mission.targetCount ? tr(`Nhận ${mission.rewardCoin} Dâu`, `Claim ${mission.rewardCoin} Dâu`) : tr('Hoàn thành nhiệm vụ', 'Complete mission')}</button>
      </article>
    })}</div>
    {!loading && <Pagination page={safePage} totalItems={missions.length} pageSize={PAGE_SIZE} onChange={setPage} />}
    {translating && <div className="translation-progress">{tr('Đang dịch nội dung...', 'Translating content...')}</div>}
  </main></div>
}
