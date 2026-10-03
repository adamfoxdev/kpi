using Kpi.Api.Domain;

namespace Kpi.Api.Data;

/// <summary>Demo data so a fresh install has a populated scorecard. Deterministic.</summary>
public static class Seeder
{
    public static void Seed(KpiDbContext db)
    {
        if (db.Departments.Any()) return;
        var rng = new Random(42);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        Department D(string n, string d) { var x = new Department { Name = n, Description = d }; db.Departments.Add(x); return x; }
        var sales = D("Sales", "Revenue generation and pipeline");
        var fin = D("Finance", "Cash, margin and cost control");
        var ops = D("Operations", "Delivery quality and speed");
        var people = D("People", "Hiring, retention and engagement");
        var cs = D("Customer Success", "Satisfaction and retention");

        // (name, desc, unit, dir, freq, target, owner, dept, start, drift per period, noise)
        var defs = new (string, string, string, Direction, Frequency, decimal, string, Department, decimal, decimal, decimal)[]
        {
            ("Monthly Recurring Revenue","Total MRR at month end","$",Direction.HigherIsBetter,Frequency.Monthly,500000,"A. Chen",sales,380000,9000,12000),
            ("Win Rate","Closed-won / closed deals","%",Direction.HigherIsBetter,Frequency.Monthly,30,"A. Chen",sales,24,0.5m,2),
            ("Sales Cycle Length","Average days lead to close","days",Direction.LowerIsBetter,Frequency.Monthly,45,"R. Patel",sales,58,-1.2m,3),
            ("Gross Margin","Gross profit / revenue","%",Direction.HigherIsBetter,Frequency.Monthly,68,"L. Moreau",fin,70,-0.3m,1.2m),
            ("Operating Expenses","Monthly opex","$",Direction.LowerIsBetter,Frequency.Monthly,320000,"L. Moreau",fin,330000,-1500,8000),
            ("Days Sales Outstanding","Average collection period","days",Direction.LowerIsBetter,Frequency.Monthly,40,"J. Okafor",fin,52,-0.8m,2.5m),
            ("On-Time Delivery","Orders delivered by promised date","%",Direction.HigherIsBetter,Frequency.Weekly,95,"S. Ito",ops,93,0.05m,1.5m),
            ("Defect Rate","Defective units per 1,000","per 1k",Direction.LowerIsBetter,Frequency.Weekly,5,"S. Ito",ops,7,-0.08m,0.7m),
            ("Avg Ticket Resolution","Hours to resolve a support ticket","hrs",Direction.LowerIsBetter,Frequency.Weekly,8,"M. Silva",cs,11,-0.1m,1.2m),
            ("Employee Turnover","Annualised voluntary attrition","%",Direction.LowerIsBetter,Frequency.Quarterly,12,"K. Novak",people,16,-0.9m,1),
            ("Time to Hire","Days from req open to offer accepted","days",Direction.LowerIsBetter,Frequency.Monthly,35,"K. Novak",people,44,-0.6m,3),
            ("Net Promoter Score","Customer NPS","pts",Direction.HigherIsBetter,Frequency.Monthly,50,"M. Silva",cs,38,1.1m,3),
            ("Customer Churn","Monthly logo churn","%",Direction.LowerIsBetter,Frequency.Monthly,2m,"M. Silva",cs,3.2m,-0.06m,0.3m),
        };

        foreach (var (name, desc, unit, dir, freq, target, owner, dept, start, drift, noise) in defs)
        {
            var k = new Domain.Kpi { Name = name, Description = desc, Unit = unit, Direction = dir, Frequency = freq,
                Target = target, Owner = owner, Department = dept, WarningThreshold = 0.9m };
            int n = freq == Frequency.Quarterly ? 6 : freq == Frequency.Weekly ? 12 : 12;
            for (int i = 0; i < n; i++)
            {
                var back = n - 1 - i;
                var date = freq switch
                {
                    Frequency.Weekly => today.AddDays(-7 * back),
                    Frequency.Quarterly => firstOfMonth.AddMonths(-3 * back),
                    _ => firstOfMonth.AddMonths(-back),
                };
                var v = start + drift * i + (decimal)(rng.NextDouble() * 2 - 1) * noise;
                k.Entries.Add(new KpiEntry { Date = date, Value = Math.Round(Math.Max(0, v), 2) });
            }
            db.Kpis.Add(k);
        }
        // One deliberately stale KPI so the stale-data flag is visible in the demo.
        var stale = new Domain.Kpi { Name = "Training Hours per Employee", Unit = "hrs", Direction = Direction.HigherIsBetter,
            Frequency = Frequency.Monthly, Target = 4, Owner = "K. Novak", Department = people, WarningThreshold = 0.9m,
            Description = "Average learning hours per employee per month" };
        for (int i = 0; i < 4; i++)
            stale.Entries.Add(new KpiEntry { Date = firstOfMonth.AddMonths(-8 - i), Value = 2.5m + i * 0.2m });
        db.Kpis.Add(stale);
        db.SaveChanges();
    }
}
