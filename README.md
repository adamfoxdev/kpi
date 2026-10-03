# KPI Scorecard

A business system built around KPIs: define metrics with targets, record readings over time, and see at a glance what is on track, at risk, or off track.

- **Backend** – ASP.NET Core 8 minimal API + EF Core, stored in a local SQLite file (`backend/Kpi.Api/kpi.db`, created and seeded with demo data on first run)
- **Frontend** – React + TypeScript (Vite), Recharts

## Run

```bash
# API  → http://localhost:5188
cd backend/Kpi.Api && ASPNETCORE_URLS=http://localhost:5188 dotnet run

# UI   → http://localhost:5173 (proxies /api to the API)
cd frontend && npm install && npm run dev
```

Tests: `cd backend && dotnet test`. Set `SeedDemoData=false` (env var) to start with an empty database.

## How KPIs are scored

- **Attainment** = value ÷ target (higher-is-better) or target ÷ value (lower-is-better); 100% means on target in either direction.
- **Status**: ≥ 100% *On track*; between the KPI's warning threshold (default 90%) and 100% *At risk*; below that *Off track*; no readings → *No data*.
- **Trend** compares the latest reading to the previous one and respects direction (a falling cost is "improving").
- **Overdue** flags a KPI whose last reading is older than two reporting periods.
- Overall attainment averages active KPIs, capping each at 150% so one outlier can't hide failures.

## Sustainment tracking

Any KPI can have a sustainment plan (go-live date, pre-improvement baseline, monitoring window of 1-36 months, owner, control-plan notes). Only readings on/after go-live are judged, using the KPI's normal status rules:

| State | Meaning |
|---|---|
| Pending | No readings since go-live yet |
| Monitoring | Latest reading on target, but the window hasn't finished cleanly |
| Sustained | Window elapsed with no off-track reading in it, and the latest reading is on target |
| Slipping | Latest reading is in the at-risk band |
| Relapsed | Latest reading is off track (also applies after the window ends) |

Also shown: on-target streak, readings on target since go-live, and *gain retained* = (latest − baseline) ÷ (target − baseline). The Sustainment page lists all plans worst-first; the scorecard has a "gains slipping / lost" tile. Databases created before this feature are upgraded automatically on startup.

## Alerts

An alert is raised when a sustainment plan **enters** Slipping or Relapsed. Each plan remembers the state it was last seen in, so re-checking never duplicates an alert.

- Slipping → Relapsed raises a new (critical) alert and resolves the slip one.
- Recovery (or removing the plan, or deactivating the KPI) resolves open alerts automatically; resolved alerts stay in History.
- Falling into Slipping again later is a new alert.
- Acknowledging an alert marks it as seen but leaves it open until the KPI actually recovers.
- Checks run when readings, KPIs or plans change, at startup, and whenever alerts are listed. The UI shows a nav badge (unacknowledged count, refreshed every minute), a scorecard banner and an Alerts page.

Alerts are in-app only; there is no email/chat delivery yet.

## Features

Scorecard dashboard (summary tiles, department health, filterable KPI cards with sparklines, worst-first), KPI detail with history-vs-target chart and reading entry, KPI create/edit/delete, department management.

## API

`GET /api/dashboard` · `GET|POST /api/kpis` · `GET|PUT|DELETE /api/kpis/{id}` · `POST /api/kpis/{id}/entries` · `DELETE /api/kpis/{id}/entries/{entryId}` · `GET /api/sustainment` · `PUT|DELETE /api/kpis/{id}/sustainment` · `GET /api/alerts[?status=all]` · `POST /api/alerts/{id}/acknowledge` · `POST /api/alerts/acknowledge-all` · `GET|POST /api/departments` · `PUT|DELETE /api/departments/{id}`
