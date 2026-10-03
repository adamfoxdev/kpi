using Kpi.Api.Data;
using Kpi.Api.Domain;
using Kpi.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kpi.Tests;

public sealed class AlertServiceTests : IDisposable
{
    // Lower-is-better, target 40, threshold 0.9: <=40 on track, 40-44.4 at risk, >44.4 off track.
    static readonly DateOnly Today = new(2026, 10, 3);
    readonly SqliteConnection conn = new("Data Source=:memory:");
    readonly KpiDbContext db;
    readonly AlertService svc;
    readonly int kpiId;

    public AlertServiceTests()
    {
        conn.Open();
        db = new KpiDbContext(new DbContextOptionsBuilder<KpiDbContext>().UseSqlite(conn).Options);
        db.Database.EnsureCreated();
        var dept = new Department { Name = "Ops" };
        var k = new Kpi.Api.Domain.Kpi
        {
            Name = "DSO", Unit = "days", Direction = Direction.LowerIsBetter, Frequency = Frequency.Monthly, Target = 40,
            WarningThreshold = 0.9m, Department = dept,
            Sustainment = new SustainmentPlan { GoLiveDate = new DateOnly(2026, 6, 1), BaselineValue = 52, MonitoringMonths = 6 },
        };
        db.Kpis.Add(k); db.SaveChanges(); kpiId = k.Id;
        svc = new AlertService(db);
    }

    public void Dispose() { db.Dispose(); conn.Dispose(); }

    void Read(string date, decimal v) { db.Entries.Add(new KpiEntry { KpiId = kpiId, Date = DateOnly.Parse(date), Value = v }); db.SaveChanges(); }
    Task Sync() => svc.SyncAsync(Today);
    Task<List<AlertDto>> Open() => svc.ListAsync(openOnly: true);

    [Fact]
    public async Task On_target_raises_nothing()
    {
        Read("2026-07-01", 38); await Sync();
        Assert.Empty(await Open());
    }

    [Fact]
    public async Task Slipping_raises_one_alert_and_resync_does_not_duplicate()
    {
        Read("2026-07-01", 38); Read("2026-08-01", 42);
        await Sync(); await Sync(); await Sync();
        var a = Assert.Single(await Open());
        Assert.Equal(SustainmentState.Slipping, a.State);
        Assert.Contains("slipping", a.Message);
        Assert.Contains("42 days", a.Message);
        Assert.Single(db.Alerts);
    }

    [Fact]
    public async Task Escalation_resolves_the_slip_and_opens_a_relapse()
    {
        Read("2026-07-01", 42); await Sync();
        Read("2026-08-01", 50); await Sync();
        var open = Assert.Single(await Open());
        Assert.Equal(SustainmentState.Relapsed, open.State);
        Assert.Equal(SustainmentState.Slipping, open.PreviousState);
        Assert.Equal(2, db.Alerts.Count());
        Assert.Contains("relapsed", open.Message);
    }

    [Fact]
    public async Task Recovery_resolves_open_alerts_and_keeps_history()
    {
        Read("2026-07-01", 50); await Sync();
        Read("2026-08-01", 38); await Sync();
        Assert.Empty(await Open());
        var all = await svc.ListAsync(openOnly: false);
        Assert.NotNull(Assert.Single(all).ResolvedAt);
    }

    [Fact]
    public async Task A_second_slip_after_recovery_is_a_new_alert()
    {
        Read("2026-07-01", 42); await Sync();
        Read("2026-08-01", 38); await Sync();
        Read("2026-09-01", 43); await Sync();
        Assert.Equal(2, db.Alerts.Count());
        Assert.Single(await Open());
    }

    [Fact]
    public async Task Removing_the_plan_resolves_its_alerts()
    {
        Read("2026-07-01", 50); await Sync();
        db.SustainmentPlans.RemoveRange(db.SustainmentPlans); db.SaveChanges();
        await Sync();
        Assert.Empty(await Open());
    }

    [Fact]
    public async Task Deactivating_the_kpi_resolves_its_alerts()
    {
        Read("2026-07-01", 50); await Sync();
        (await db.Kpis.FindAsync(kpiId))!.IsActive = false; db.SaveChanges();
        await Sync();
        Assert.Empty(await Open());
    }

    [Fact]
    public async Task Acknowledge_marks_alert_but_keeps_it_open()
    {
        Read("2026-07-01", 50); await Sync();
        var a = Assert.Single(await Open());
        Assert.Equal(1, await svc.AcknowledgeAsync(a.Id));
        var after = Assert.Single(await Open());
        Assert.NotNull(after.AcknowledgedAt);
        Assert.Equal(0, await svc.AcknowledgeAsync(null)); // nothing left to acknowledge
    }
}
