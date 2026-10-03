import { useState } from 'react'
import { api, type Kpi, type Sustainment, type SustainmentInput } from '../api'
import { fmtPct, fmtValue, today } from '../format'
import { ErrorBox, SustainBadge } from './Bits'
import { Modal } from './KpiForm'

const HELP: Record<Sustainment['state'], string> = {
  Pending: 'No readings on or after go-live yet.',
  Monitoring: 'On target, but the monitoring window has not finished cleanly yet.',
  Sustained: 'The monitoring window finished with no off-track readings and the KPI is on target.',
  Slipping: 'The latest reading has dropped into the at-risk band.',
  Relapsed: 'The latest reading is off track — the gain has been lost. Re-open the improvement work.',
}

export function SustainmentPanel({ kpi, plan, onChange }: { kpi: Kpi; plan?: Sustainment | null; onChange: (d: Awaited<ReturnType<typeof api.saveSustainment>>) => void }) {
  const [editing, setEditing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function remove() {
    if (!confirm('Remove the sustainment plan for this KPI? Readings are kept.')) return
    try { onChange(await api.deleteSustainment(kpi.id)) } catch (e) { setError((e as Error).message) }
  }

  return (
    <section className="panel">
      <div className="head">
        <h2>Sustainment {plan && <SustainBadge state={plan.state} />}</h2>
        <div className="actions">
          <button className="btn" onClick={() => setEditing(true)}>{plan ? 'Edit plan' : 'Start sustainment tracking'}</button>
          {plan && <button className="btn danger" onClick={remove}>Remove</button>}
        </div>
      </div>
      <ErrorBox error={error} />
      {!plan && <p className="muted">Track whether the improvement holds after go-live: set a baseline, a monitoring window and a control plan, and the system will tell you when the gain is sustained or slipping.</p>}
      {plan && (
        <>
          <p className="muted small">{HELP[plan.state]}</p>
          <dl className="meta">
            <div><dt>Go-live</dt><dd>{plan.goLiveDate}</dd></div>
            <div><dt>Monitoring ends</dt><dd>{plan.monitoringEnds} {plan.daysRemaining > 0 ? `(${plan.daysRemaining}d left)` : '(complete)'}</dd></div>
            <div><dt>Baseline → target</dt><dd>{fmtValue(plan.baselineValue, plan.unit)} → {fmtValue(plan.target, plan.unit)}</dd></div>
            <div><dt>Gain retained</dt><dd>{fmtPct(plan.gainRetained)}</dd></div>
            <div><dt>On target since go-live</dt><dd>{plan.onTarget} of {plan.readingsSince} readings</dd></div>
            <div><dt>Current streak</dt><dd>{plan.streak} on target</dd></div>
            <div><dt>Sustainment owner</dt><dd>{plan.owner || '—'}</dd></div>
          </dl>
          {plan.controlPlan && <><div className="muted small">Control plan</div><p className="pre">{plan.controlPlan}</p></>}
        </>
      )}
      {editing && <PlanForm kpi={kpi} plan={plan} onClose={() => setEditing(false)} onSaved={d => { setEditing(false); onChange(d) }} />}
    </section>
  )
}

function PlanForm({ kpi, plan, onClose, onSaved }: { kpi: Kpi; plan?: Sustainment | null; onClose: () => void; onSaved: (d: Awaited<ReturnType<typeof api.saveSustainment>>) => void }) {
  const [f, setF] = useState<SustainmentInput>(plan ?? {
    goLiveDate: today(), baselineValue: kpi.spark[0]?.value ?? 0, monitoringMonths: 6, owner: kpi.owner ?? '', controlPlan: '',
  })
  const [error, setError] = useState<string | null>(null)
  const set = <K extends keyof SustainmentInput>(k: K, v: SustainmentInput[K]) => setF({ ...f, [k]: v })

  async function submit(e: React.FormEvent) {
    e.preventDefault(); setError(null)
    try { onSaved(await api.saveSustainment(kpi.id, f)) } catch (err) { setError((err as Error).message) }
  }
  return (
    <Modal title={plan ? 'Edit sustainment plan' : 'Start sustainment tracking'} onClose={onClose}>
      <form className="form" onSubmit={submit}>
        <ErrorBox error={error} />
        <div className="row">
          <label>Go-live date<input type="date" required value={f.goLiveDate} onChange={e => set('goLiveDate', e.target.value)} /></label>
          <label>Monitoring period (months)<input type="number" min={1} max={36} required value={f.monitoringMonths} onChange={e => set('monitoringMonths', +e.target.value)} /></label>
        </div>
        <label>Baseline before improvement{kpi.unit && ` (${kpi.unit})`}
          <input type="number" step="any" required value={f.baselineValue} onChange={e => set('baselineValue', +e.target.value)} />
          <span className="muted small">Used to measure how much of the gain is retained. Target is {fmtValue(kpi.target, kpi.unit)}.</span>
        </label>
        <label>Sustainment owner<input value={f.owner ?? ''} onChange={e => set('owner', e.target.value)} /></label>
        <label>Control plan<textarea value={f.controlPlan ?? ''} onChange={e => set('controlPlan', e.target.value)} placeholder="What keeps the gain in place? Checks, reviews, escalation triggers…" /></label>
        <div className="actions">
          <button type="button" className="btn" onClick={onClose}>Cancel</button>
          <button className="btn primary">Save plan</button>
        </div>
      </form>
    </Modal>
  )
}
