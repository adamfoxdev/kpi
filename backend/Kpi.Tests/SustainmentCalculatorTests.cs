using Kpi.Api.Domain;
using Kpi.Api.Services;
using Xunit;

namespace Kpi.Tests;

public class SustainmentCalculatorTests
{
    static readonly DateOnly GoLive = new(2026, 4, 1);
    static readonly DateOnly Today = new(2026, 10, 3);

    // Lower-is-better KPI: target 40, baseline 52, threshold 0.9 (so >44.4 is off track, 40-44.4 at risk).
    static SustainmentResult Eval(int months, params (string date, decimal v)[] r) =>
        SustainmentCalculator.Evaluate(Direction.LowerIsBetter, 40, 0.9m, 52, GoLive, months,
            r.Select(x => (DateOnly.Parse(x.date), x.v)), Today);

    [Fact]
    public void No_readings_since_go_live_is_pending() =>
        Assert.Equal(SustainmentState.Pending, Eval(6, ("2026-03-01", 38)).State); // pre go-live reading ignored

    [Fact]
    public void On_target_inside_window_is_monitoring()
    {
        var r = Eval(12, ("2026-05-01", 39), ("2026-06-01", 38));
        Assert.Equal(SustainmentState.Monitoring, r.State);
        Assert.Equal(2, r.Streak);
    }

    [Fact]
    public void On_target_after_clean_window_is_sustained() =>
        Assert.Equal(SustainmentState.Sustained, Eval(3, ("2026-05-01", 39), ("2026-08-01", 39), ("2026-09-01", 38)).State);

    [Fact]
    public void Off_track_reading_inside_window_blocks_sustained()
    {
        // Failed in June (inside the window) then recovered: on track now, but the gain is not yet proven.
        var r = Eval(6, ("2026-05-01", 39), ("2026-06-01", 50), ("2026-09-01", 39));
        Assert.Equal(SustainmentState.Monitoring, r.State);
        Assert.Equal(1, r.Streak);
    }

    [Fact]
    public void Latest_at_risk_is_slipping() =>
        Assert.Equal(SustainmentState.Slipping, Eval(6, ("2026-05-01", 39), ("2026-09-01", 42)).State);

    [Fact]
    public void Latest_off_track_is_relapsed_even_after_window() =>
        Assert.Equal(SustainmentState.Relapsed, Eval(3, ("2026-05-01", 39), ("2026-09-01", 50)).State);

    [Fact]
    public void Gain_retained_is_direction_agnostic()
    {
        // baseline 52 -> target 40 is a 12-point gain; latest 46 keeps half of it.
        Assert.Equal(0.5m, Eval(6, ("2026-09-01", 46)).GainRetained);
        var higher = SustainmentCalculator.Evaluate(Direction.HigherIsBetter, 100, 0.9m, 60, GoLive, 6,
            [(new DateOnly(2026, 9, 1), 80m)], Today);
        Assert.Equal(0.5m, higher.GainRetained);
    }

    [Fact]
    public void Baseline_equal_to_target_has_no_retained_figure() =>
        Assert.Null(SustainmentCalculator.Evaluate(Direction.HigherIsBetter, 10, 0.9m, 10, GoLive, 6,
            [(new DateOnly(2026, 9, 1), 10m)], Today).GainRetained);
}
