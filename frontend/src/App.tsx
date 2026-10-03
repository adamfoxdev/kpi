import { useCallback, useEffect, useState } from 'react'
import { NavLink, Route, Routes } from 'react-router-dom'
import { api } from './api'
import { ALERTS_CHANGED } from './alertsBus'
import Alerts from './pages/Alerts'
import Dashboard from './pages/Dashboard'
import Departments from './pages/Departments'
import KpiDetail from './pages/KpiDetail'
import Manage from './pages/Manage'
import Sustainment from './pages/Sustainment'

export default function App() {
  const [unacked, setUnacked] = useState(0)
  const refresh = useCallback(() => {
    api.alerts().then(a => setUnacked(a.filter(x => !x.acknowledgedAt).length)).catch(() => { /* badge is best-effort */ })
  }, [])
  useEffect(() => {
    refresh()
    const t = setInterval(refresh, 60_000)
    window.addEventListener(ALERTS_CHANGED, refresh)
    return () => { clearInterval(t); window.removeEventListener(ALERTS_CHANGED, refresh) }
  }, [refresh])

  return (
    <>
      <header className="top">
        <strong className="brand">◆ KPI Scorecard</strong>
        <nav>
          <NavLink to="/" end>Scorecard</NavLink>
          <NavLink to="/kpis">All KPIs</NavLink>
          <NavLink to="/sustainment">Sustainment</NavLink>
          <NavLink to="/alerts">Alerts{unacked > 0 && <span className="count" aria-label={`${unacked} unacknowledged`}>{unacked}</span>}</NavLink>
          <NavLink to="/departments">Departments</NavLink>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/kpis" element={<Manage />} />
          <Route path="/kpis/:id" element={<KpiDetail />} />
          <Route path="/sustainment" element={<Sustainment />} />
          <Route path="/alerts" element={<Alerts />} />
          <Route path="/departments" element={<Departments />} />
          <Route path="*" element={<p>Not found.</p>} />
        </Routes>
      </main>
    </>
  )
}
