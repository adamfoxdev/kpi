using Kpi.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Api.Data;

public class KpiDbContext(DbContextOptions<KpiDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Domain.Kpi> Kpis => Set<Domain.Kpi>();
    public DbSet<KpiEntry> Entries => Set<KpiEntry>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<SustainmentPlan> SustainmentPlans => Set<SustainmentPlan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Department>().HasIndex(d => d.Name).IsUnique();
        b.Entity<Domain.Kpi>().Property(k => k.Direction).HasConversion<string>();
        b.Entity<Domain.Kpi>().Property(k => k.Frequency).HasConversion<string>();
        b.Entity<Domain.Kpi>().Property(k => k.Target).HasPrecision(18, 4);
        b.Entity<Domain.Kpi>().Property(k => k.WarningThreshold).HasPrecision(5, 4);
        b.Entity<Domain.Kpi>().HasOne(k => k.Department).WithMany(d => d.Kpis)
            .HasForeignKey(k => k.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<KpiEntry>().Property(e => e.Value).HasPrecision(18, 4);
        b.Entity<KpiEntry>().HasIndex(e => new { e.KpiId, e.Date }).IsUnique();
        b.Entity<KpiEntry>().HasOne<Domain.Kpi>().WithMany(k => k.Entries)
            .HasForeignKey(e => e.KpiId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SustainmentPlan>().Property(p => p.BaselineValue).HasPrecision(18, 4);
        b.Entity<SustainmentPlan>().HasIndex(p => p.KpiId).IsUnique();
        b.Entity<Domain.Kpi>().HasOne(k => k.Sustainment).WithOne().HasForeignKey<SustainmentPlan>(p => p.KpiId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<SustainmentPlan>().Property(p => p.LastState).HasConversion<string>();
        b.Entity<Alert>().Property(a => a.State).HasConversion<string>();
        b.Entity<Alert>().Property(a => a.PreviousState).HasConversion<string>();
        b.Entity<Alert>().HasIndex(a => new { a.KpiId, a.ResolvedAt });
        b.Entity<Alert>().HasOne<Domain.Kpi>().WithMany().HasForeignKey(a => a.KpiId).OnDelete(DeleteBehavior.Cascade);
    }
}
