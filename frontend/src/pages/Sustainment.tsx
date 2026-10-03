import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, type Sustainment as S, type SustainmentState } from '../api'
import { ErrorBox, SustainBadge } from '../components/Bits'
import { fmtPct, fmtValue, SUSTAIN_LABEL, SUSTAIN_TONE } from '../format'

const STATES: SustainmentState[] = ['Relapsed', 'Slipping', 'Monitoring', 'Pending', 'Sustained']

export default function Sustainment() {
  const [rows, setRows] = useState<S[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState<SustainmentState | ''>('')
  useEffect(() => { api.sustainment().then(setRows).catch(e => setError(e.message)) }, [])

  if (error) return <ErrorBox error={error} />
  if (!rows) return <p className="muted">Loading…</p>
  const shown = rows.filter(r => !filter || r.state === filter)

  return (
    <>
      <h1>Sustainment</h1>
      <p className="muted">Are the gains from improvement work holding? Each plan is judged on readings since go-live.</p>
      <div className="tiles">
        {STATES.map(st => (
          <button key={st} className={`tile s-${SUSTAIN_TONE[st]} ${filter === st ? 'active' : ''}`} onClick={() => setFilter(filter === st ? '' : st)}>
            <span className="tile-v">{rows.filter(r => r.state === st).length}</span><span className="tile-l">{SUSTAIN_LABEL[st]}</span>
          </button>
        ))}
      </div>
      {rows.length === 0 ? (
        <p className="muted">No sustainment plans yet. Open a KPI and choose “Start sustainment tracking”.</p>
      ) : (
        <table>
          <thead><tr><th>KPI</th><th>State</th><th>Monitoring window</th><th className="num">Latest / target</th><th className="num">Gain retained</th><th className="num">Streak</th><th>Owner</th></tr></thead>
          <tbody>
            {shown.map(r => (
              <tr key={r.kpiId}>
                <td><Link to={`/kpis/${r.kpiId}`}>{r.kpiName}</Link><div className="muted small">{r.departmentName}{r.isStale && ' · ⚠ overdue reading'}</div></td>
                <td><SustainBadge state={r.state} /></td>
                <td>
                  <div className="progress thin" role="progressbar" aria-valuenow={Math.round(r.percentElapsed * 100)} aria-valuemin={0} aria-valuemax={100}>
                    <div style={{ width: `${r.percentElapsed * 100}%`, background: 'var(--c-Info)' }} />
                  </div>
                  <span className="muted small">{r.daysRemaining > 0 ? `${r.daysRemaining}d left` : 'complete'} · from {r.goLiveDate}</span>
                </td>
                <td className="num">{fmtValue(r.latestValue, r.unit)} / {fmtValue(r.target, r.unit)}</td>
                <td className="num">{fmtPct(r.gainRetained)}</td>
                <td className="num">{r.streak}/{r.readingsSince}</td>
                <td>{r.owner}</td>
              </tr>
            ))}
            {shown.length === 0 && <tr><td colSpan={7} className="muted">No plans in this state.</td></tr>}
          </tbody>
        </table>
      )}
    </>
  )
}
