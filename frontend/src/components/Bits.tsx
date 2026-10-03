import { Line, LineChart, ResponsiveContainer, YAxis } from 'recharts'
import type { Kpi, Status, SustainmentState } from '../api'
import { STATUS_LABEL, SUSTAIN_LABEL, SUSTAIN_TONE } from '../format'

const ICON: Record<Status, string> = { OnTrack: '●', AtRisk: '▲', OffTrack: '■', NoData: '○' }

export function StatusBadge({ status }: { status: Status }) {
  return <span className={`badge s-${status}`}><span aria-hidden>{ICON[status]}</span> {STATUS_LABEL[status]}</span>
}

const S_ICON: Record<SustainmentState, string> = { Pending: '○', Monitoring: '◔', Sustained: '✔', Slipping: '▲', Relapsed: '■' }

export function SustainBadge({ state }: { state: SustainmentState }) {
  return <span className={`badge s-${SUSTAIN_TONE[state]}`}><span aria-hidden>{S_ICON[state]}</span> {SUSTAIN_LABEL[state]}</span>
}

export function Spark({ kpi }: { kpi: Kpi }) {
  if (kpi.spark.length < 2) return <div className="spark muted small">Not enough data</div>
  const vals = kpi.spark.map(p => p.value).concat(kpi.target)
  return (
    <div className="spark" aria-hidden>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={kpi.spark} margin={{ top: 4, right: 2, bottom: 4, left: 2 }}>
          <YAxis hide domain={[Math.min(...vals), Math.max(...vals)]} />
          <Line type="monotone" dataKey="value" stroke={`var(--c-${kpi.status})`} strokeWidth={2} dot={false} isAnimationActive={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}

export function Progress({ value, status }: { value?: number; status: Status }) {
  const pct = Math.max(0, Math.min(1, value ?? 0)) * 100
  return (
    <div className="progress" role="progressbar" aria-valuenow={Math.round(pct)} aria-valuemin={0} aria-valuemax={100}>
      <div style={{ width: `${pct}%`, background: `var(--c-${status})` }} />
    </div>
  )
}

export function ErrorBox({ error }: { error: string | null }) {
  return error ? <div className="error" role="alert">{error}</div> : null
}
