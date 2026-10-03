import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, type Alert } from '../api'
import { notifyAlertsChanged } from '../alertsBus'
import { ErrorBox, SustainBadge } from '../components/Bits'

const ago = (iso: string) => {
  const m = Math.max(0, Math.round((Date.now() - new Date(iso).getTime()) / 60000))
  if (m < 1) return 'just now'
  if (m < 60) return `${m}m ago`
  if (m < 60 * 24) return `${Math.round(m / 60)}h ago`
  return `${Math.round(m / 1440)}d ago`
}

export default function Alerts() {
  const [rows, setRows] = useState<Alert[] | null>(null)
  const [history, setHistory] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(() => api.alerts(history).then(setRows).catch(e => setError(e.message)), [history])
  useEffect(() => { load() }, [load])

  async function ack(id?: number) {
    setError(null)
    try { await (id ? api.acknowledgeAlert(id) : api.acknowledgeAll()); await load(); notifyAlertsChanged() }
    catch (e) { setError((e as Error).message) }
  }

  if (!rows) return error ? <ErrorBox error={error} /> : <p className="muted">Loading…</p>
  const unacked = rows.filter(r => !r.resolvedAt && !r.acknowledgedAt).length

  return (
    <>
      <div className="head">
        <div>
          <h1>Alerts</h1>
          <p className="muted">Raised when a sustainment plan starts slipping or relapses. They clear automatically when the KPI recovers.</p>
        </div>
        <div className="actions">
          <button className={`btn ${history ? '' : 'primary'}`} onClick={() => setHistory(false)}>Open</button>
          <button className={`btn ${history ? 'primary' : ''}`} onClick={() => setHistory(true)}>History</button>
          {unacked > 1 && <button className="btn" onClick={() => ack()}>Acknowledge all ({unacked})</button>}
        </div>
      </div>
      <ErrorBox error={error} />
      {rows.length === 0 && <p className="muted">{history ? 'No alerts have been raised yet.' : '✓ No open alerts — every sustainment plan is holding.'}</p>}
      <ul className="alerts">
        {rows.map(a => (
          <li key={a.id} className={`alert a-${a.state} ${a.resolvedAt ? 'resolved' : ''}`}>
            <div className="alert-main">
              <div className="alert-top">
                <SustainBadge state={a.state} />
                {a.previousState && <span className="muted small">was {a.previousState.toLowerCase()}</span>}
                <span className="muted small" title={new Date(a.createdAt).toLocaleString()}>{ago(a.createdAt)}</span>
                {a.resolvedAt && <span className="badge s-OnTrack">Resolved {ago(a.resolvedAt)}</span>}
                {!a.resolvedAt && a.acknowledgedAt && <span className="muted small">Acknowledged {ago(a.acknowledgedAt)}</span>}
              </div>
              <p className="alert-msg">{a.message}</p>
              <div className="muted small"><Link to={`/kpis/${a.kpiId}`}>Open {a.kpiName}</Link> · {a.departmentName}{a.owner && ` · Owner ${a.owner}`}</div>
            </div>
            {!a.resolvedAt && !a.acknowledgedAt && <button className="btn" onClick={() => ack(a.id)}>Acknowledge</button>}
          </li>
        ))}
      </ul>
    </>
  )
}
