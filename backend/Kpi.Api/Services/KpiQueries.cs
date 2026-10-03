using Kpi.Api.Data;
using Kpi.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Api.Services;

public class KpiQueries(KpiDbContext db)
{
    public static KpiSummaryDto Summarise(Domain.Kpi k, DateOnly today)
    {
        var ordered = k.Entries.OrderBy(e => e.Date).ToList();
        var latest = ordered.LastOrDefault();
        var prev = ordered.Count > 1 ? ordered[^2] : null;
        decimal? att = latest is null ? null : KpiCalculator.Attainment(k.Direction, latest.Value, k.Target);
        var status = att is null ? KpiStatus.NoData : KpiCalculator.Status(att.Value, k.WarningThreshold);
        decimal? delta = null; bool? improving = null;
        if (latest is not null && prev is not null)
        {
            var t = KpiCalculator.Trend(k.Direction, latest.Value, prev.Value);
            delta = t.Delta; improving = t.Improving;
        }
        return new KpiSummaryDto(
            k.Id, k.Name, k.Description, k.Unit, k.Direction, k.Frequency, k.Target, k.WarningThreshold,
            k.Owner, k.IsActive, k.DepartmentId, k.Department?.Name ?? "",
            latest?.Value, latest?.Date, att, status, delta, improving,
            latest is not null && KpiCalculator.IsStale(k.Frequency, latest.Date, today),
            ordered.TakeLast(12).Select(e => new PointDto(e.Date, e.Value)).ToList());
    }

    public static SustainmentDto? BuildSustainment(Domain.Kpi k, DateOnly today)
    {
        var p = k.Sustainment;
        if (p is null) return null;
        var r = SustainmentCalculator.Evaluate(k.Direction, k.Target, k.WarningThreshold, p.BaselineValue,
            p.GoLiveDate, p.MonitoringMonths, k.Entries.Select(e => (e.Date, e.Value)), today);
        var end = p.GoLiveDate.AddMonths(p.MonitoringMonths);
        var total = Math.Max(1, end.DayNumber - p.GoLiveDate.DayNumber);
        var elapsed = Math.Clamp((decimal)(today.DayNumber - p.GoLiveDate.DayNumber) / total, 0m, 1m);
        var latest = k.Entries.OrderByDescending(e => e.Date).FirstOrDefault();
        return new SustainmentDto(k.Id, k.Name, k.Department?.Name ?? "", k.Unit, k.Direction, k.Target, latest?.Value,
            p.BaselineValue, p.GoLiveDate, end, p.MonitoringMonths, p.Owner, p.ControlPlan, r.State,
            r.ReadingsSince, r.OnTarget, r.Streak, r.GainRetained, Math.Max(0, end.DayNumber - today.DayNumber), elapsed,
            latest is not null && KpiCalculator.IsStale(k.Frequency, latest.Date, today));
    }

    /// <summary>Active KPIs that have a sustainment plan, worst state first.</summary>
    public async Task<List<SustainmentDto>> SustainmentAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var kpis = await db.Kpis.AsNoTracking().Include(k => k.Department).Include(k => k.Entries).Include(k => k.Sustainment)
            .Where(k => k.IsActive && k.Sustainment != null).ToListAsync(ct);
        return kpis.Select(k => BuildSustainment(k, today)!)
            .OrderBy(s => s.State switch { SustainmentState.Relapsed => 0, SustainmentState.Slipping => 1, SustainmentState.Monitoring => 2, SustainmentState.Pending => 3, _ => 4 })
            .ThenBy(s => s.KpiName).ToList();
    }

    public async Task<List<KpiSummaryDto>> AllAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var kpis = await db.Kpis.AsNoTracking().Include(k => k.Department).Include(k => k.Entries)
            .OrderBy(k => k.Department!.Name).ThenBy(k => k.Name).ToListAsync(ct);
        return kpis.Select(k => Summarise(k, today)).ToList();
    }

    public async Task<DashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var all = (await AllAsync(ct)).Where(k => k.IsActive).ToList();
        int Count(IEnumerable<KpiSummaryDto> s, KpiStatus st) => s.Count(k => k.Status == st);
        // Cap each KPI's attainment at 150% so one outlier can't mask several failing KPIs.
        decimal? Avg(IEnumerable<KpiSummaryDto> s)
        {
            var v = s.Where(k => k.Attainment is not null).Select(k => Math.Min(k.Attainment!.Value, 1.5m)).ToList();
            return v.Count == 0 ? null : v.Average();
        }
        var gainsAtRisk = (await SustainmentAsync(ct)).Count(x => x.State is SustainmentState.Slipping or SustainmentState.Relapsed);
        var depts = await db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);
        var health = depts.Select(d =>
        {
            var s = all.Where(k => k.DepartmentId == d.Id).ToList();
            return new DepartmentHealthDto(d.Id, d.Name, s.Count, Count(s, KpiStatus.OnTrack),
                Count(s, KpiStatus.AtRisk), Count(s, KpiStatus.OffTrack), Count(s, KpiStatus.NoData), Avg(s));
        }).ToList();
        return new DashboardDto(all.Count, Count(all, KpiStatus.OnTrack), Count(all, KpiStatus.AtRisk),
            Count(all, KpiStatus.OffTrack), Count(all, KpiStatus.NoData), all.Count(k => k.IsStale), gainsAtRisk,
            Avg(all), health, all);
    }

    public async Task<KpiDetailDto?> DetailAsync(int id, CancellationToken ct = default)
    {
        var k = await db.Kpis.AsNoTracking().Include(x => x.Department).Include(x => x.Entries).Include(x => x.Sustainment)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (k is null) return null;
        var entries = k.Entries.OrderByDescending(e => e.Date).Select(e => new EntryDto(e.Id, e.Date, e.Value, e.Note)).ToList();
        // Detail chart wants the full history, not just the last 12 points.
        var summary = Summarise(k, DateOnly.FromDateTime(DateTime.Today)) with
        {
            Spark = k.Entries.OrderBy(e => e.Date).Select(e => new PointDto(e.Date, e.Value)).ToList()
        };
        return new KpiDetailDto(summary, entries, BuildSustainment(k, DateOnly.FromDateTime(DateTime.Today)));
    }
}
