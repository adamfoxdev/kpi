using Microsoft.EntityFrameworkCore;

namespace Kpi.Api.Data;

/// <summary>
/// There are no EF migrations: EnsureCreated builds a fresh database, and this brings databases created by
/// earlier versions up to date. Every step is idempotent.
/// </summary>
public static class SchemaUpgrade
{
    public static void Apply(KpiDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "SustainmentPlans" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SustainmentPlans" PRIMARY KEY AUTOINCREMENT,
                "KpiId" INTEGER NOT NULL, "GoLiveDate" TEXT NOT NULL, "BaselineValue" TEXT NOT NULL,
                "MonitoringMonths" INTEGER NOT NULL, "Owner" TEXT NULL, "ControlPlan" TEXT NULL, "LastState" TEXT NULL,
                CONSTRAINT "FK_SustainmentPlans_Kpis_KpiId" FOREIGN KEY ("KpiId") REFERENCES "Kpis" ("Id") ON DELETE CASCADE);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_SustainmentPlans_KpiId" ON "SustainmentPlans" ("KpiId");
            CREATE TABLE IF NOT EXISTS "Alerts" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Alerts" PRIMARY KEY AUTOINCREMENT,
                "KpiId" INTEGER NOT NULL, "State" TEXT NOT NULL, "PreviousState" TEXT NULL, "Message" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL, "AcknowledgedAt" TEXT NULL, "ResolvedAt" TEXT NULL,
                CONSTRAINT "FK_Alerts_Kpis_KpiId" FOREIGN KEY ("KpiId") REFERENCES "Kpis" ("Id") ON DELETE CASCADE);
            CREATE INDEX IF NOT EXISTS "IX_Alerts_KpiId_ResolvedAt" ON "Alerts" ("KpiId", "ResolvedAt");
            """);

        // Column added after the first sustainment release.
        var hasLastState = db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('SustainmentPlans') WHERE name = 'LastState'")
            .AsEnumerable().First() > 0;
        if (!hasLastState)
            db.Database.ExecuteSqlRaw("ALTER TABLE \"SustainmentPlans\" ADD COLUMN \"LastState\" TEXT NULL");
    }
}
