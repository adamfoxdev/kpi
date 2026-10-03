import { NavLink, Route, Routes } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import Departments from './pages/Departments'
import KpiDetail from './pages/KpiDetail'
import Manage from './pages/Manage'
import Sustainment from './pages/Sustainment'

export default function App() {
  return (
    <>
      <header className="top">
        <strong className="brand">◆ KPI Scorecard</strong>
        <nav>
          <NavLink to="/" end>Scorecard</NavLink>
          <NavLink to="/kpis">All KPIs</NavLink>
          <NavLink to="/sustainment">Sustainment</NavLink>
          <NavLink to="/departments">Departments</NavLink>
        </nav>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/kpis" element={<Manage />} />
          <Route path="/kpis/:id" element={<KpiDetail />} />
          <Route path="/sustainment" element={<Sustainment />} />
          <Route path="/departments" element={<Departments />} />
          <Route path="*" element={<p>Not found.</p>} />
        </Routes>
      </main>
    </>
  )
}
