using Kpi.Api.Domain;
using Kpi.Api.Services;
using Xunit;

namespace Kpi.Tests;

public class KpiCalculatorTests
{
    [Theory]
    [InlineData(Direction.HigherIsBetter, 90, 100, 0.9)]
    [InlineData(Direction.HigherIsBetter, 120, 100, 1.2)]
    [InlineData(Direction.LowerIsBetter, 50, 40, 0.8)]   // 10 over target when lower is better
    [InlineData(Direction.LowerIsBetter, 20, 40, 2.0)]
    [InlineData(Direction.LowerIsBetter, 0, 40, 1.0)]    // zero is perfect, no divide-by-zero
    [InlineData(Direction.HigherIsBetter, 5, 0, 1.0)]    // zero target is guarded
    public void Attainment_respects_direction(Direction d, double value, double target, double expected) =>
        Assert.Equal((decimal)expected, KpiCalculator.Attainment(d, (decimal)value, (decimal)target));

    [Theory]
    [InlineData(1.0, KpiStatus.OnTrack)]
    [InlineData(1.3, KpiStatus.OnTrack)]
    [InlineData(0.95, KpiStatus.AtRisk)]
    [InlineData(0.9, KpiStatus.AtRisk)]
    [InlineData(0.89, KpiStatus.OffTrack)]
    public void Status_uses_threshold(double attainment, KpiStatus expected) =>
        Assert.Equal(expected, KpiCalculator.Status((decimal)attainment, 0.9m));

    [Fact]
    public void Trend_improving_depends_on_direction()
    {
        Assert.True(KpiCalculator.Trend(Direction.HigherIsBetter, 10, 8).Improving);
        Assert.False(KpiCalculator.Trend(Direction.LowerIsBetter, 10, 8).Improving);
        Assert.True(KpiCalculator.Trend(Direction.LowerIsBetter, 6, 8).Improving);
        Assert.Null(KpiCalculator.Trend(Direction.HigherIsBetter, 8, 8).Improving);
    }

    [Fact]
    public void Stale_after_two_periods()
    {
        var today = new DateOnly(2026, 10, 3);
        Assert.False(KpiCalculator.IsStale(Frequency.Weekly, today.AddDays(-14), today));
        Assert.True(KpiCalculator.IsStale(Frequency.Weekly, today.AddDays(-15), today));
        Assert.False(KpiCalculator.IsStale(Frequency.Monthly, today.AddDays(-60), today));
        Assert.True(KpiCalculator.IsStale(Frequency.Monthly, today.AddDays(-63), today));
    }
}
