import type { Kpi, Status, SustainmentState } from './api'

export function fmtValue(v: number | undefined | null, unit: string): string {
  if (v === undefined || v === null) return '—'
  const abs = Math.abs(v)
  const n = abs >= 1_000_000 ? `${(v / 1_000_000).toFixed(2)}M`
    : abs >= 10_000 ? `${(v / 1000).toFixed(1).replace(/\.0$/, "")}k`
    : v.toLocaleString(undefined, { maximumFractionDigits: 2 })
  if (unit === '$') return `$${n}`
  if (unit === '%') return `${n}%`
  return unit ? `${n} ${unit}` : n
}

export const fmtPct = (v?: number | null) => (v === undefined || v === null ? '—' : `${Math.round(v * 100)}%`)

export const STATUS_LABEL: Record<Status, string> = {
  OnTrack: 'On track', AtRisk: 'At risk', OffTrack: 'Off track', NoData: 'No data',
}

export function trendText(k: Kpi): string {
  if (k.delta === undefined || k.delta === null) return 'No prior reading'
  if (k.delta === 0) return 'Flat vs previous'
  const sign = k.delta > 0 ? '▲' : '▼'
  return `${sign} ${fmtValue(Math.abs(k.delta), k.unit)} vs previous`
}

export const today = () => new Date().toISOString().slice(0, 10)

export const SUSTAIN_LABEL: Record<SustainmentState, string> = {
  Pending: 'Pending', Monitoring: 'Monitoring', Sustained: 'Sustained', Slipping: 'Slipping', Relapsed: 'Relapsed',
}
/** Which existing status colour family each sustainment state borrows. */
export const SUSTAIN_TONE: Record<SustainmentState, string> = {
  Pending: 'NoData', Monitoring: 'Info', Sustained: 'OnTrack', Slipping: 'AtRisk', Relapsed: 'OffTrack',
}
