import { useEffect, useState } from 'react'
import { api, type Department, type Kpi, type KpiInput } from '../api'
import { ErrorBox } from './Bits'

const blank = (departmentId: number): KpiInput => ({
  name: '', description: '', unit: '', direction: 'HigherIsBetter', frequency: 'Monthly',
  target: 0, warningThreshold: 0.9, owner: '', isActive: true, departmentId,
})

export function KpiForm({ kpi, onClose, onSaved }: { kpi?: Kpi; onClose: () => void; onSaved: (id: number) => void }) {
  const [depts, setDepts] = useState<Department[]>([])
  const [f, setF] = useState<KpiInput | null>(kpi ? { ...kpi } : null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api.departments().then(d => {
      setDepts(d)
      setF(cur => cur ?? blank(d[0]?.id ?? 0))
    }).catch(e => setError(e.message))
  }, [])

  if (!f) return <Modal title="KPI" onClose={onClose}><ErrorBox error={error} /><p className="muted">Loading…</p></Modal>
  const set = <K extends keyof KpiInput>(k: K, v: KpiInput[K]) => setF({ ...f, [k]: v })

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    if (!f) return
    setBusy(true); setError(null)
    try {
      const saved = kpi ? await api.updateKpi(kpi.id, f) : await api.createKpi(f)
      onSaved(saved.kpi.id)
    } catch (err) { setError((err as Error).message) } finally { setBusy(false) }
  }

  return (
    <Modal title={kpi ? 'Edit KPI' : 'New KPI'} onClose={onClose}>
      <form onSubmit={submit} className="form">
        <ErrorBox error={error} />
        <label>Name<input required value={f.name} onChange={e => set('name', e.target.value)} /></label>
        <label>Description<input value={f.description ?? ''} onChange={e => set('description', e.target.value)} /></label>
        <div className="row">
          <label>Department
            <select value={f.departmentId} onChange={e => set('departmentId', +e.target.value)}>
              {depts.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </label>
          <label>Owner<input value={f.owner ?? ''} onChange={e => set('owner', e.target.value)} /></label>
        </div>
        <div className="row">
          <label>Target<input type="number" step="any" required value={f.target} onChange={e => set('target', +e.target.value)} /></label>
          <label>Unit<input placeholder="$, %, days…" value={f.unit} onChange={e => set('unit', e.target.value)} /></label>
        </div>
        <div className="row">
          <label>Better when
            <select value={f.direction} onChange={e => set('direction', e.target.value as KpiInput['direction'])}>
              <option value="HigherIsBetter">Higher</option><option value="LowerIsBetter">Lower</option>
            </select>
          </label>
          <label>Reported
            <select value={f.frequency} onChange={e => set('frequency', e.target.value as KpiInput['frequency'])}>
              {['Daily', 'Weekly', 'Monthly', 'Quarterly'].map(x => <option key={x}>{x}</option>)}
            </select>
          </label>
        </div>
        <label>At-risk threshold: {Math.round(f.warningThreshold * 100)}% of target
          <input type="range" min={0.5} max={0.99} step={0.01} value={f.warningThreshold}
            onChange={e => set('warningThreshold', +e.target.value)} />
          <span className="muted small">Between this and 100% of target is “at risk”; below it is “off track”.</span>
        </label>
        <label className="check"><input type="checkbox" checked={f.isActive} onChange={e => set('isActive', e.target.checked)} /> Active (included in dashboard)</label>
        <div className="actions">
          <button type="button" className="btn" onClick={onClose}>Cancel</button>
          <button className="btn primary" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  )
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) {
  useEffect(() => {
    const h = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', h)
    return () => window.removeEventListener('keydown', h)
  }, [onClose])
  return (
    <div className="overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title}>
        <h2>{title}</h2>
        {children}
      </div>
    </div>
  )
}
