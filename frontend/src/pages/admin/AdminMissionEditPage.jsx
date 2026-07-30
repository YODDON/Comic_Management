import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { getAdminMissions, updateMission } from '../../services/missionService'
import { useLanguage } from '../../contexts/LanguageContext'

const types = [['Đọc chương truyện', 'Read chapters'], ['Mua chương truyện', 'Unlock chapters'], ['Bình luận', 'Comment'], ['Treo ở sảnh (phút)', 'Lobby time (minutes)']]

function toDateTimeLocal(value) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16)
}

export default function AdminMissionEditPage() {
  const { language, tr } = useLanguage()
  const { id } = useParams()
  const navigate = useNavigate()
  const [form, setForm] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    async function load() {
      try {
        const result = await getAdminMissions()
        const mission = (Array.isArray(result?.data) ? result.data : [])
          .find((item) => item.id === id)
        if (!mission) throw new Error(tr('Không tìm thấy nhiệm vụ cần chỉnh sửa.', 'The mission could not be found.'))
        if (active) {
          setForm({
            ...mission,
            type: Number(mission.type),
            startDate: toDateTimeLocal(mission.startDate),
            endDate: toDateTimeLocal(mission.endDate),
          })
        }
      } catch (requestError) {
        if (active) setError(language === 'en' ? 'Unable to load the mission.' : requestError.message)
      } finally {
        if (active) setLoading(false)
      }
    }
    load()
    return () => { active = false }
  }, [id, language, tr])

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await updateMission(id, {
        ...form,
        type: Number(form.type),
        targetCount: Number(form.targetCount),
        rewardCoin: Number(form.rewardCoin),
        startDate: new Date(form.startDate).toISOString(),
        endDate: form.endDate ? new Date(form.endDate).toISOString() : null,
      })
      navigate('/admin/missions', {
        replace: true,
        state: { message: tr('Đã cập nhật nhiệm vụ.', 'Mission updated.') },
      })
    } catch (requestError) {
      setError(language === 'en' ? 'Unable to update the mission.' : requestError.message)
    } finally {
      setSaving(false)
    }
  }

  return <AdminLayout title={tr('Chỉnh sửa nhiệm vụ', 'Edit mission')}>
    <div className="admin-page-heading">
      <div><h2>{tr('Chỉnh sửa nhiệm vụ', 'Edit mission')}</h2><p>{tr('Cập nhật nội dung, mục tiêu và phần thưởng của nhiệm vụ.', 'Update the mission content, target and reward.')}</p></div>
    </div>
    {error && <div className="admin-alert error">{error}</div>}
    {loading && <div className="admin-form-page">{tr('Đang tải nhiệm vụ...', 'Loading mission...')}</div>}
    {!loading && form && <form className="admin-form-page mission-create-page mission-edit-page" onSubmit={submit}>
      <label>{tr('Tên nhiệm vụ', 'Mission title')}<input autoFocus required maxLength="200" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} /></label>
      <label>{tr('Mô tả', 'Description')}<textarea required value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></label>
      <div className="mission-form-grid">
        <label>{tr('Loại nhiệm vụ', 'Mission type')}<select value={form.type} onChange={(event) => setForm({ ...form, type: Number(event.target.value) })}>{types.map((type, index) => <option value={index} key={type[0]}>{tr(...type)}</option>)}</select></label>
        <label>{Number(form.type) === 3 ? tr('Số phút ở sảnh', 'Minutes in lobby') : tr('Số lần cần hoàn thành', 'Required count')}<input type="number" min="1" max={Number(form.type) === 3 ? 5 : undefined} required value={form.targetCount} onChange={(event) => setForm({ ...form, targetCount: event.target.value })} /></label>
        <label>{tr('Phần thưởng (Dâu)', 'Reward (Dâu)')}<input type="number" min="0" step="0.01" required value={form.rewardCoin} onChange={(event) => setForm({ ...form, rewardCoin: event.target.value })} /></label>
        <label>{tr('Ngày bắt đầu', 'Start date')}<input type="datetime-local" required value={form.startDate} onChange={(event) => setForm({ ...form, startDate: event.target.value })} /></label>
        <label>{tr('Ngày kết thúc', 'End date')}<input type="datetime-local" value={form.endDate} min={form.startDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} /></label>
        <label className="mission-check"><input type="checkbox" checked={Boolean(form.isActive)} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} /> {tr('Đang hoạt động', 'Active')}</label>
      </div>
      <footer>
        <button type="button" onClick={() => navigate('/admin/missions')}>{tr('Hủy', 'Cancel')}</button>
        <button className="save" disabled={saving}>{saving ? tr('Đang lưu...', 'Saving...') : tr('Lưu nhiệm vụ', 'Save mission')}</button>
      </footer>
    </form>}
  </AdminLayout>
}
