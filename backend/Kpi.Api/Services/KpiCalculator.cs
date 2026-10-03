using Kpi.Api.Domain;

namespace Kpi.Api.Services;

/// <summary>Pure KPI maths: attainment, RAG status, trend and staleness.</summary>
public static class KpiCalculator
{
    /// <summary>1.0 = exactly on target, above 1.0 = beating it, whichever direction is "better".</summary>
    public static decimal Attainment(Direction direction, decimal value, decimal target)
    {
        if (direction == Direction.HigherIsBetter)
            return target == 0 ? (value >= 0 ? 1m : 0m) : value / target;
        // Lower is better: at or below target is >= 1.
        if (value <= 0) return 1m;
        return target / value;
    }

    public static KpiStatus Status(decimal attainment, decimal warningThreshold) =>
        attainment >= 1m ? KpiStatus.OnTrack
        : attainment >= warningThreshold ? KpiStatus.AtRisk
        : KpiStatus.OffTrack;

    /// <summary>Signed change vs the previous value; "improving" respects the KPI direction.</summary>
    public static (decimal Delta, bool? Improving) Trend(Direction direction, decimal latest, decimal previous)
    {
        var delta = latest - previous;
        if (delta == 0) return (0, null);
        var up = delta > 0;
        return (delta, direction == Direction.HigherIsBetter ? up : !up);
    }

    public static int PeriodDays(Frequency f) => f switch
    {
        Frequency.Daily => 1,
        Frequency.Weekly => 7,
        Frequency.Monthly => 31,
        Frequency.Quarterly => 92,
        _ => 31
    };

    /// <summary>Stale when the last reading is older than two reporting periods.</summary>
    public static bool IsStale(Frequency f, DateOnly lastEntry, DateOnly today) =>
        today.DayNumber - lastEntry.DayNumber > PeriodDays(f) * 2;
}
