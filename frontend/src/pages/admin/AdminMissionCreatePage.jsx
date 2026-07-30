import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import AdminLayout from '../../components/AdminLayout'
import { createMission } from '../../services/missionService'
import { useLanguage } from '../../contexts/LanguageContext'

const types = [['Đọc chương truyện', 'Read chapters'], ['Mua chương truyện', 'Unlock chapters'], ['Bình luận', 'Comment'], ['Treo ở sảnh (phút)', 'Lobby time (minutes)']]
const initialForm = () => ({
  title: '', description: '', rewardCoin: 5, type: 0, targetCount: 1,
  startDate: new Date().toISOString().slice(0, 16), endDate: '',
})

export default function AdminMissionCreatePage() {
  const { language, tr } = useLanguage()
  const navigate = useNavigate()
  const [form, setForm] = useState(initialForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await createMission({
        ...form,
        type: Number(form.type),
        targetCount: Number(form.targetCount),
        rewardCoin: Number(form.rewardCoin),
        startDate: new Date(form.startDate).toISOString(),
        endDate: form.endDate ? new Date(form.endDate).toISOString() : null,
      })
      navigate('/admin/missions', { replace: true, state: { message: tr('Đã thêm nhiệm vụ mới.', 'New mission added.') } })
    } catch (requestError) { setError(language === 'en' ? 'Unable to create the mission.' : requestError.message) }
    finally { setSaving(false) }
  }

  return <AdminLayout title={tr('Thêm nhiệm vụ', 'Add mission')}>
    <div className="admin-page-heading"><div><h2>{tr('Thêm nhiệm vụ mới', 'Add a new mission')}</h2><p>{tr('Thiết lập nội dung, mục tiêu và phần thưởng Dâu cho người dùng.', 'Configure the content, target and Dâu reward for users.')}</p></div></div>
    {error && <div className="admin-alert error">{error}</div>}
    <form className="admin-form-page mission-create-page" onSubmit={submit}>
      <label>{tr('Tên nhiệm vụ', 'Mission title')}<input autoFocus required maxLength="200" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder={tr('Ví dụ: Đọc truyện One Piece', 'Example: Read One Piece')} /></label>
      <label>{tr('Mô tả', 'Description')}<textarea required value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder={tr('Mô tả điều kiện hoàn thành nhiệm vụ', 'Describe the mission completion requirements')} /></label>
      <div className="mission-form-grid">
        <label>{tr('Loại nhiệm vụ', 'Mission type')}<select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value })}>{types.map((type, index) => <option value={index} key={type[0]}>{tr(...type)}</option>)}</select></label>
        <label>{Number(form.type) === 3 ? tr('Số phút ở sảnh', 'Minutes in lobby') : tr('Số lần cần hoàn thành', 'Required count')}<input type="number" min="1" max={Number(form.type) === 3 ? 5 : undefined} required value={form.targetCount} onChange={(e) => setForm({ ...form, targetCount: e.target.value })} /></label>
        <label>{tr('Phần thưởng (Dâu)', 'Reward (Dâu)')}<input type="number" min="0" step="0.01" required value={form.rewardCoin} onChange={(e) => setForm({ ...form, rewardCoin: e.target.value })} /></label>
        <label>{tr('Ngày bắt đầu', 'Start date')}<input type="datetime-local" required value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></label>
        <label>{tr('Ngày kết thúc', 'End date')}<input type="datetime-local" value={form.endDate} min={form.startDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></label>
      </div>
      <footer><button type="button" onClick={() => navigate('/admin/missions')}>{tr('Hủy', 'Cancel')}</button><button className="save" disabled={saving}>{saving ? tr('Đang lưu...', 'Saving...') : tr('Thêm nhiệm vụ', 'Add mission')}</button></footer>
    </form>
  </AdminLayout>
}
