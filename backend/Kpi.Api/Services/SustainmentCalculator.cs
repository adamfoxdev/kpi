using Kpi.Api.Domain;

namespace Kpi.Api.Services;

public record SustainmentResult(SustainmentState State, int ReadingsSince, int OnTarget, int Streak, decimal? GainRetained);

/// <summary>
/// Pure sustainment logic. Looks only at readings on/after go-live and judges them with the KPI's normal status rules:
/// latest Off track → Relapsed; latest At risk → Slipping; latest On track → Sustained once the monitoring window
/// has elapsed with no Off-track reading inside it, otherwise Monitoring; no readings yet → Pending.
/// </summary>
public static class SustainmentCalculator
{
    public static SustainmentResult Evaluate(
        Direction direction, decimal target, decimal warningThreshold, decimal baseline,
        DateOnly goLive, int monitoringMonths, IEnumerable<(DateOnly Date, decimal Value)> readings, DateOnly today)
    {
        var monitorEnd = goLive.AddMonths(monitoringMonths);
        var since = readings.Where(r => r.Date >= goLive && r.Date <= today).OrderBy(r => r.Date)
            .Select(r => (r.Date, r.Value, Status: KpiCalculator.Status(KpiCalculator.Attainment(direction, r.Value, target), warningThreshold)))
            .ToList();

        if (since.Count == 0) return new(SustainmentState.Pending, 0, 0, 0, null);

        var latest = since[^1];
        var streak = since.AsEnumerable().Reverse().TakeWhile(r => r.Status == KpiStatus.OnTrack).Count();
        var onTarget = since.Count(r => r.Status == KpiStatus.OnTrack);
        // (latest - baseline) / (target - baseline) is direction-agnostic: both terms flip sign together.
        decimal? retained = target == baseline ? null : (latest.Value - baseline) / (target - baseline);

        var windowClean = !since.Any(r => r.Date <= monitorEnd && r.Status == KpiStatus.OffTrack);
        var state = latest.Status switch
        {
            KpiStatus.OffTrack => SustainmentState.Relapsed,
            KpiStatus.AtRisk => SustainmentState.Slipping,
            _ => today >= monitorEnd && windowClean ? SustainmentState.Sustained : SustainmentState.Monitoring,
        };
        return new(state, since.Count, onTarget, streak, retained);
    }
}
