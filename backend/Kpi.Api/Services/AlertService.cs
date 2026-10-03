using Kpi.Api.Data;
using Kpi.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Api.Services;

public record AlertDto(
    int Id, int KpiId, string KpiName, string DepartmentName, string? Owner, SustainmentState State,
    SustainmentState? PreviousState, string Message, DateTime CreatedAt, DateTime? AcknowledgedAt, DateTime? ResolvedAt);

/// <summary>
/// Turns sustainment state changes into alerts. <see cref="SyncAsync"/> is idempotent: it compares each plan's
/// current state with the state recorded at the last sync, so calling it repeatedly never duplicates an alert.
/// </summary>
public class AlertService(KpiDbContext db)
{
    static bool IsAlertState(SustainmentState s) => s is SustainmentState.Slipping or SustainmentState.Relapsed;

    public async Task SyncAsync(DateOnly? today = null, DateTime? nowUtc = null, CancellationToken ct = default)
    {
        var day = today ?? DateOnly.FromDateTime(DateTime.Today);
        var now = nowUtc ?? DateTime.UtcNow;
        var kpis = await db.Kpis.Include(k => k.Department).Include(k => k.Entries).Include(k => k.Sustainment)
            .Where(k => k.Sustainment != null).ToListAsync(ct);
        var open = (await db.Alerts.Where(a => a.ResolvedAt == null).ToListAsync(ct)).ToLookup(a => a.KpiId);

        foreach (var k in kpis)
        {
            var plan = k.Sustainment!;
            var dto = KpiQueries.BuildSustainment(k, day)!;
            // An inactive KPI is out of scope: treat it as having no alertable state.
            SustainmentState? current = k.IsActive ? dto.State : null;
            if (plan.LastState == current) continue;

            var mine = open[k.Id].ToList();
            foreach (var a in mine.Where(a => current is null || !IsAlertState(current.Value) || a.State != current))
                a.ResolvedAt = now;

            if (current is { } c && IsAlertState(c) && !mine.Any(a => a.State == c && a.ResolvedAt is null))
                db.Alerts.Add(new Alert { KpiId = k.Id, State = c, PreviousState = plan.LastState, CreatedAt = now, Message = Describe(k, dto) });

            plan.LastState = current;
        }

        // A removed plan has nothing left to alert about.
        var planned = kpis.Select(k => k.Id).ToHashSet();
        foreach (var a in open.Where(g => !planned.Contains(g.Key)).SelectMany(g => g)) a.ResolvedAt = now;

        await db.SaveChangesAsync(ct);
    }

    static string Describe(Domain.Kpi k, SustainmentDto d)
    {
        var verb = d.State == SustainmentState.Relapsed ? "has relapsed" : "is slipping";
        var retained = d.GainRetained is { } r ? $", {Math.Round(r * 100)}% of the gain retained" : "";
        return $"{k.Name} {verb}: latest {Fmt(d.LatestValue, d.Unit)} vs target {Fmt(d.Target, d.Unit)}{retained}.";
    }

    static string Fmt(decimal? v, string unit)
    {
        if (v is null) return "—";
        var n = v.Value.ToString("0.##");
        return unit switch { "$" => $"${n}", "%" => $"{n}%", "" => n, _ => $"{n} {unit}" };
    }

    public async Task<List<AlertDto>> ListAsync(bool openOnly, CancellationToken ct = default)
    {
        var q = db.Alerts.AsNoTracking().AsQueryable();
        if (openOnly) q = q.Where(a => a.ResolvedAt == null);
        var alerts = await q.ToListAsync(ct);
        var kpis = await db.Kpis.AsNoTracking().Include(k => k.Department).Include(k => k.Sustainment)
            .Where(k => alerts.Select(a => a.KpiId).Contains(k.Id)).ToDictionaryAsync(k => k.Id, ct);
        return alerts
            // Unresolved first, relapses before slips, then newest.
            .OrderBy(a => a.ResolvedAt is null ? 0 : 1).ThenBy(a => a.State == SustainmentState.Relapsed ? 0 : 1)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AlertDto(a.Id, a.KpiId, kpis[a.KpiId].Name, kpis[a.KpiId].Department?.Name ?? "",
                kpis[a.KpiId].Sustainment?.Owner ?? kpis[a.KpiId].Owner, a.State, a.PreviousState, a.Message,
                DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc),
                a.AcknowledgedAt is { } ak ? DateTime.SpecifyKind(ak, DateTimeKind.Utc) : null,
                a.ResolvedAt is { } rs ? DateTime.SpecifyKind(rs, DateTimeKind.Utc) : null)).ToList();
    }

    /// <summary>Acknowledge one alert, or every open one when <paramref name="id"/> is null. Returns how many changed.</summary>
    public async Task<int> AcknowledgeAsync(int? id, CancellationToken ct = default)
    {
        var rows = await db.Alerts.Where(a => a.AcknowledgedAt == null && (id == null || a.Id == id)).ToListAsync(ct);
        foreach (var a in rows) a.AcknowledgedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }
}
