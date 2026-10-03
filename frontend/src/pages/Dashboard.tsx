import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, type Dashboard as D, type Kpi, type Status } from '../api'
import { ErrorBox, Progress, Spark, StatusBadge } from '../components/Bits'
import { fmtPct, fmtValue, trendText } from '../format'

export default function Dashboard() {
  const [data, setData] = useState<D | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [dept, setDept] = useState(0)
  const [status, setStatus] = useState<Status | ''>('')
  const [q, setQ] = useState('')

  useEffect(() => { api.dashboard().then(setData).catch(e => setError(e.message)) }, [])

  const shown = useMemo(() => (data?.kpis ?? []).filter(k =>
    (!dept || k.departmentId === dept) && (!status || k.status === status) &&
    (!q || (k.name + k.owner).toLowerCase().includes(q.toLowerCase()))
  ).sort((a, b) => rank(a) - rank(b) || a.name.localeCompare(b.name)), [data, dept, status, q])

  if (error) return <ErrorBox error={error} />
  if (!data) return <p className="muted">Loading…</p>

  const tile = (label: string, value: string | number, cls: string, s?: Status) => (
    <button className={`tile ${cls} ${status === s && s ? 'active' : ''}`} onClick={() => s && setStatus(status === s ? '' : s)} disabled={!s}>
      <span className="tile-v">{value}</span><span className="tile-l">{label}</span>
    </button>
  )

  return (
    <>
      <h1>Scorecard</h1>
      <div className="tiles">
        {tile('Overall attainment', fmtPct(data.overallAttainment), 'overall')}
        {tile('On track', data.onTrack, 's-OnTrack', 'OnTrack')}
        {tile('At risk', data.atRisk, 's-AtRisk', 'AtRisk')}
        {tile('Off track', data.offTrack, 's-OffTrack', 'OffTrack')}
        {tile('No data', data.noData, 's-NoData', 'NoData')}
        {tile('Stale (overdue)', data.stale, 'stale')}
      </div>

      <section className="panel">
        <h2>Department health</h2>
        <div className="depts">
          {data.departments.filter(d => d.total > 0).map(d => (
            <button key={d.id} className={`dept ${dept === d.id ? 'active' : ''}`} onClick={() => setDept(dept === d.id ? 0 : d.id)}>
              <span className="dept-name">{d.name}</span>
              <span className="stack" role="img" aria-label={`${d.onTrack} on track, ${d.atRisk} at risk, ${d.offTrack} off track`}>
                {(['onTrack', 'atRisk', 'offTrack', 'noData'] as const).map(k => d[k] > 0 &&
                  <span key={k} style={{ flex: d[k], background: `var(--c-${k[0].toUpperCase() + k.slice(1)})` }} />)}
              </span>
              <span className="dept-pct">{fmtPct(d.avgAttainment)}</span>
            </button>
          ))}
        </div>
      </section>

      <div className="filters">
        <input type="search" placeholder="Search KPI or owner…" value={q} onChange={e => setQ(e.target.value)} />
        <select value={dept} onChange={e => setDept(+e.target.value)} aria-label="Department">
          <option value={0}>All departments</option>
          {data.departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
        </select>
        <select value={status} onChange={e => setStatus(e.target.value as Status | '')} aria-label="Status">
          <option value="">All statuses</option>
          <option value="OffTrack">Off track</option><option value="AtRisk">At risk</option>
          <option value="OnTrack">On track</option><option value="NoData">No data</option>
        </select>
        <span className="muted small">{shown.length} of {data.kpis.length} KPIs</span>
      </div>

      <div className="grid">
        {shown.map(k => (
          <Link key={k.id} to={`/kpis/${k.id}`} className={`card b-${k.status}`}>
            <div className="card-top"><span className="muted small">{k.departmentName}</span><StatusBadge status={k.status} /></div>
            <h3>{k.name}</h3>
            <div className="big">{fmtValue(k.latestValue, k.unit)}</div>
            <div className="muted small">Target {fmtValue(k.target, k.unit)} · {k.direction === 'HigherIsBetter' ? 'higher' : 'lower'} is better</div>
            <Progress value={k.attainment} status={k.status} />
            <div className="card-foot">
              <span className={`small ${k.improving === true ? 'good' : k.improving === false ? 'bad' : 'muted'}`}>{trendText(k)}</span>
              <span className="small">{fmtPct(k.attainment)}</span>
            </div>
            <Spark kpi={k} />
            {k.isStale && <div className="stale-tag">⚠ Overdue — last reading {k.latestDate}</div>}
          </Link>
        ))}
        {shown.length === 0 && <p className="muted">No KPIs match these filters.</p>}
      </div>
    </>
  )
}

// Worst first so problems surface at the top of the scorecard.
const rank = (k: Kpi) => ({ OffTrack: 0, AtRisk: 1, NoData: 2, OnTrack: 3 })[k.status]
