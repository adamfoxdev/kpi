export type Direction = 'HigherIsBetter' | 'LowerIsBetter'
export type Frequency = 'Daily' | 'Weekly' | 'Monthly' | 'Quarterly'
export type Status = 'OnTrack' | 'AtRisk' | 'OffTrack' | 'NoData'

export interface Point { date: string; value: number }
export interface Kpi {
  id: number; name: string; description?: string; unit: string
  direction: Direction; frequency: Frequency; target: number; warningThreshold: number
  owner?: string; isActive: boolean; departmentId: number; departmentName: string
  latestValue?: number; latestDate?: string; attainment?: number; status: Status
  delta?: number; improving?: boolean | null; isStale: boolean; spark: Point[]
}
export interface Entry { id: number; date: string; value: number; note?: string }
export interface KpiDetail { kpi: Kpi; entries: Entry[] }
export interface Department { id: number; name: string; description?: string; kpiCount: number }
export interface DeptHealth {
  id: number; name: string; total: number; onTrack: number; atRisk: number; offTrack: number
  noData: number; avgAttainment?: number
}
export interface Dashboard {
  total: number; onTrack: number; atRisk: number; offTrack: number; noData: number; stale: number
  overallAttainment?: number; departments: DeptHealth[]; kpis: Kpi[]
}
export interface KpiInput {
  name: string; description?: string; unit: string; direction: Direction; frequency: Frequency
  target: number; warningThreshold: number; owner?: string; isActive: boolean; departmentId: number
}

async function call<T>(method: string, url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    headers: body ? { 'content-type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })
  if (!res.ok) {
    let msg = `${res.status} ${res.statusText}`
    try {
      const p = await res.json()
      msg = Object.values(p.errors ?? {}).flat().join(' ') || p.title || msg
    } catch { /* non-JSON error body */ }
    throw new Error(msg)
  }
  return res.status === 204 ? (undefined as T) : res.json()
}

export const api = {
  dashboard: () => call<Dashboard>('GET', '/api/dashboard'),
  kpis: () => call<Kpi[]>('GET', '/api/kpis'),
  kpi: (id: number) => call<KpiDetail>('GET', `/api/kpis/${id}`),
  createKpi: (i: KpiInput) => call<KpiDetail>('POST', '/api/kpis', i),
  updateKpi: (id: number, i: KpiInput) => call<KpiDetail>('PUT', `/api/kpis/${id}`, i),
  deleteKpi: (id: number) => call<void>('DELETE', `/api/kpis/${id}`),
  addEntry: (id: number, e: { date: string; value: number; note?: string }) =>
    call<KpiDetail>('POST', `/api/kpis/${id}/entries`, e),
  deleteEntry: (id: number, entryId: number) =>
    call<KpiDetail>('DELETE', `/api/kpis/${id}/entries/${entryId}`),
  departments: () => call<Department[]>('GET', '/api/departments'),
  saveDepartment: (id: number | null, d: { name: string; description?: string }) =>
    id ? call<Department>('PUT', `/api/departments/${id}`, d) : call<Department>('POST', '/api/departments', d),
  deleteDepartment: (id: number) => call<void>('DELETE', `/api/departments/${id}`),
}
