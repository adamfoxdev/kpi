import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, type Kpi } from '../api'
import { ErrorBox, StatusBadge } from '../components/Bits'
import { KpiForm } from '../components/KpiForm'
import { fmtValue } from '../format'

export default function Manage() {
  const nav = useNavigate()
  const [kpis, setKpis] = useState<Kpi[]>([])
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const load = useCallback(() => api.kpis().then(setKpis).catch(e => setError(e.message)), [])
  useEffect(() => { load() }, [load])

  return (
    <>
      <div className="head"><h1>All KPIs</h1><button className="btn primary" onClick={() => setCreating(true)}>+ New KPI</button></div>
      <ErrorBox error={error} />
      <table>
        <thead><tr><th>KPI</th><th>Department</th><th>Owner</th><th>Frequency</th><th className="num">Latest</th><th className="num">Target</th><th>Status</th></tr></thead>
        <tbody>
          {kpis.map(k => (
            <tr key={k.id} className={k.isActive ? '' : 'inactive'}>
              <td><Link to={`/kpis/${k.id}`}>{k.name}</Link>{!k.isActive && <span className="muted small"> (inactive)</span>}</td>
              <td>{k.departmentName}</td><td>{k.owner}</td><td>{k.frequency}</td>
              <td className="num">{fmtValue(k.latestValue, k.unit)}</td><td className="num">{fmtValue(k.target, k.unit)}</td>
              <td><StatusBadge status={k.status} /></td>
            </tr>
          ))}
        </tbody>
      </table>
      {creating && <KpiForm onClose={() => setCreating(false)} onSaved={id => nav(`/kpis/${id}`)} />}
    </>
  )
}
