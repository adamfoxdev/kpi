using System.ComponentModel.DataAnnotations;
using Kpi.Api.Domain;

namespace Kpi.Api.Services;

public record DepartmentInput([Required, StringLength(80)] string Name, string? Description);
public record DepartmentDto(int Id, string Name, string? Description, int KpiCount);

public record KpiInput(
    [Required, StringLength(120)] string Name, string? Description, [StringLength(20)] string Unit,
    Direction Direction, Frequency Frequency, decimal Target,
    [Range(0.01, 1)] decimal WarningThreshold, string? Owner, bool IsActive, int DepartmentId);

public record EntryInput(DateOnly Date, decimal Value, string? Note);
public record EntryDto(int Id, DateOnly Date, decimal Value, string? Note);
public record PointDto(DateOnly Date, decimal Value);

public record KpiSummaryDto(
    int Id, string Name, string? Description, string Unit, Direction Direction, Frequency Frequency,
    decimal Target, decimal WarningThreshold, string? Owner, bool IsActive,
    int DepartmentId, string DepartmentName,
    decimal? LatestValue, DateOnly? LatestDate, decimal? Attainment, KpiStatus Status,
    decimal? Delta, bool? Improving, bool IsStale, List<PointDto> Spark);

public record SustainmentInput(
    DateOnly GoLiveDate, decimal BaselineValue, [Range(1, 36)] int MonitoringMonths, string? Owner, string? ControlPlan);

public record SustainmentDto(
    int KpiId, string KpiName, string DepartmentName, string Unit, Direction Direction,
    decimal Target, decimal? LatestValue, decimal BaselineValue, DateOnly GoLiveDate, DateOnly MonitoringEnds,
    int MonitoringMonths, string? Owner, string? ControlPlan, SustainmentState State,
    int ReadingsSince, int OnTarget, int Streak, decimal? GainRetained,
    int DaysRemaining, decimal PercentElapsed, bool IsStale);

public record KpiDetailDto(KpiSummaryDto Kpi, List<EntryDto> Entries, SustainmentDto? Sustainment);

public record DepartmentHealthDto(int Id, string Name, int Total, int OnTrack, int AtRisk, int OffTrack, int NoData, decimal? AvgAttainment);

public record DashboardDto(
    int Total, int OnTrack, int AtRisk, int OffTrack, int NoData, int Stale, int GainsAtRisk,
    decimal? OverallAttainment, List<DepartmentHealthDto> Departments, List<KpiSummaryDto> Kpis);
