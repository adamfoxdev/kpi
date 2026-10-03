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

## Features

Scorecard dashboard (summary tiles, department health, filterable KPI cards with sparklines, worst-first), KPI detail with history-vs-target chart and reading entry, KPI create/edit/delete, department management.

## API

`GET /api/dashboard` · `GET|POST /api/kpis` · `GET|PUT|DELETE /api/kpis/{id}` · `POST /api/kpis/{id}/entries` · `DELETE /api/kpis/{id}/entries/{entryId}` · `GET|POST /api/departments` · `PUT|DELETE /api/departments/{id}`
