namespace Kpi.Api.Domain;

public enum Direction { HigherIsBetter, LowerIsBetter }
public enum Frequency { Daily, Weekly, Monthly, Quarterly }
public enum KpiStatus { OnTrack, AtRisk, OffTrack, NoData }

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<Kpi> Kpis { get; set; } = new();
}

public class Kpi
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Unit { get; set; } = "";          // e.g. "$", "%", "days", "count"
    public Direction Direction { get; set; }
    public Frequency Frequency { get; set; }
    public decimal Target { get; set; }
    /// <summary>Attainment (0-1) below which a KPI is OffTrack; between this and 1 it is AtRisk.</summary>
    public decimal WarningThreshold { get; set; } = 0.9m;
    public string? Owner { get; set; }
    public bool IsActive { get; set; } = true;
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public List<KpiEntry> Entries { get; set; } = new();
}

public class KpiEntry
{
    public int Id { get; set; }
    public int KpiId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Value { get; set; }
    public string? Note { get; set; }
}
