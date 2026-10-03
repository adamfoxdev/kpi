import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { CartesianGrid, Line, LineChart, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api, type KpiDetail as Detail } from '../api'
import { ErrorBox, Progress, StatusBadge } from '../components/Bits'
import { KpiForm } from '../components/KpiForm'
import { fmtPct, fmtValue, today, trendText } from '../format'

export default function KpiDetail() {
  const id = Number(useParams().id)
  const nav = useNavigate()
  const [d, setD] = useState<Detail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState(false)
  const [date, setDate] = useState(today())
  const [value, setValue] = useState('')
  const [note, setNote] = useState('')

  const load = useCallback(() => api.kpi(id).then(setD).catch(e => setError(e.message)), [id])
  useEffect(() => { load() }, [load])

  if (error && !d) return <ErrorBox error={error} />
  if (!d) return <p className="muted">Loading…</p>
  const { kpi: k, entries } = d
  const vals = k.spark.map(p => p.value).concat(k.target)
  const span = Math.max(...vals) - Math.min(...vals) || Math.abs(k.target) || 1
  const pad = (v: number, dir: 1 | -1) => v + dir * span * 0.12

  async function add(e: React.FormEvent) {
    e.preventDefault(); setError(null)
    try { setD(await api.addEntry(id, { date, value: Number(value), note: note || undefined })); setValue(''); setNote('') }
    catch (err) { setError((err as Error).message) }
  }
  async function del() {
    if (!confirm(`Delete "${k.name}" and all its readings?`)) return
    await api.deleteKpi(id); nav('/')
  }

  return (
    <>
      <p className="small"><Link to="/">← Scorecard</Link></p>
      <div className="head">
        <div>
          <h1>{k.name}</h1>
          <p className="muted">{k.departmentName} · Owner {k.owner || '—'} · {k.frequency} · {k.direction === 'HigherIsBetter' ? 'Higher' : 'Lower'} is better{!k.isActive && ' · Inactive'}</p>
          {k.description && <p>{k.description}</p>}
        </div>
        <div className="actions">
          <button className="btn" onClick={() => setEditing(true)}>Edit</button>
          <button className="btn danger" onClick={del}>Delete</button>
        </div>
      </div>

      <div className="tiles">
        <div className="tile"><span className="tile-v">{fmtValue(k.latestValue, k.unit)}</span><span className="tile-l">Latest{k.latestDate && ` (${k.latestDate})`}</span></div>
        <div className="tile"><span className="tile-v">{fmtValue(k.target, k.unit)}</span><span className="tile-l">Target</span></div>
        <div className="tile"><span className="tile-v">{fmtPct(k.attainment)}</span><span className="tile-l">Attainment</span></div>
        <div className="tile"><span className="tile-v"><StatusBadge status={k.status} /></span><span className="tile-l">{trendText(k)}</span></div>
      </div>
      <Progress value={k.attainment} status={k.status} />
      {k.isStale && <div className="stale-tag">⚠ This KPI is overdue for a new {k.frequency.toLowerCase()} reading.</div>}

      <section className="panel">
        <h2>History vs target</h2>
        {k.spark.length < 2 ? <p className="muted">Add at least two readings to see a trend.</p> : (
          <div className="chart">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={k.spark} margin={{ top: 8, right: 24, bottom: 0, left: 8 }}>
                <CartesianGrid stroke="var(--border)" strokeDasharray="3 3" />
                <XAxis dataKey="date" stroke="var(--muted)" fontSize={12} />
                <YAxis stroke="var(--muted)" fontSize={12} width={72} domain={[(min: number) => pad(Math.min(min, k.target), -1), (max: number) => pad(Math.max(max, k.target), 1)]} tickFormatter={v => fmtValue(v, k.unit)} />
                <Tooltip formatter={(v) => fmtValue(Number(v), k.unit)} contentStyle={{ background: 'var(--panel)', border: '1px solid var(--border)' }} />
                <ReferenceLine y={k.target} stroke="var(--c-OnTrack)" strokeDasharray="6 4" label={{ value: 'Target', fill: 'var(--muted)', fontSize: 12, position: 'insideBottomRight' }} />
                <Line type="monotone" dataKey="value" name={k.name} stroke="var(--accent)" strokeWidth={2.5} dot={{ r: 3 }} isAnimationActive={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}
      </section>

      <section className="panel">
        <h2>Record a reading</h2>
        <ErrorBox error={error} />
        <form onSubmit={add} className="inline-form">
          <label>Date<input type="date" required value={date} onChange={e => setDate(e.target.value)} /></label>
          <label>Value{k.unit && ` (${k.unit})`}<input type="number" step="any" required value={value} onChange={e => setValue(e.target.value)} /></label>
          <label className="grow">Note<input value={note} onChange={e => setNote(e.target.value)} placeholder="Optional context" /></label>
          <button className="btn primary">Save</button>
        </form>
        <p className="muted small">Saving a reading for a date that already has one replaces it.</p>
        <table>
          <thead><tr><th>Date</th><th className="num">Value</th><th>Note</th><th /></tr></thead>
          <tbody>
            {entries.map(e => (
              <tr key={e.id}>
                <td>{e.date}</td><td className="num">{fmtValue(e.value, k.unit)}</td><td>{e.note}</td>
                <td className="num"><button className="link danger" onClick={async () => setD(await api.deleteEntry(id, e.id))}>Remove</button></td>
              </tr>
            ))}
            {entries.length === 0 && <tr><td colSpan={4} className="muted">No readings yet.</td></tr>}
          </tbody>
        </table>
      </section>

      {editing && <KpiForm kpi={k} onClose={() => setEditing(false)} onSaved={() => { setEditing(false); load() }} />}
    </>
  )
}
