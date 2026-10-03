using System.Text.Json.Serialization;
using Kpi.Api.Data;
using Kpi.Api.Domain;
using Kpi.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("Kpi") ?? "Data Source=kpi.db";
builder.Services.AddDbContext<KpiDbContext>(o => o.UseSqlite(conn));
builder.Services.AddScoped<KpiQueries>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KpiDbContext>();
    db.Database.EnsureCreated();
    if (app.Configuration.GetValue("SeedDemoData", true)) Seeder.Seed(db);
}

app.UseCors();

static string? Check(KpiInput i) =>
    string.IsNullOrWhiteSpace(i.Name) ? "Name is required."
    : i.WarningThreshold is <= 0 or > 1 ? "Warning threshold must be between 0 and 1."
    : null;

static IResult Invalid(string msg) => Results.ValidationProblem(new Dictionary<string, string[]> { ["error"] = [msg] });

var api = app.MapGroup("/api");

api.MapGet("/dashboard", (KpiQueries q, CancellationToken ct) => q.DashboardAsync(ct));

// ---- Departments
api.MapGet("/departments", async (KpiDbContext db) =>
    await db.Departments.AsNoTracking().OrderBy(d => d.Name)
        .Select(d => new DepartmentDto(d.Id, d.Name, d.Description, d.Kpis.Count)).ToListAsync());

api.MapPost("/departments", async (DepartmentInput i, KpiDbContext db) =>
{
    var name = i.Name.Trim();
    if (name.Length == 0) return Invalid("Name is required.");
    if (await db.Departments.AnyAsync(d => d.Name == name)) return Invalid("A department with that name already exists.");
    var d = new Department { Name = name, Description = i.Description };
    db.Departments.Add(d); await db.SaveChangesAsync();
    return Results.Created($"/api/departments/{d.Id}", new DepartmentDto(d.Id, d.Name, d.Description, 0));
});

api.MapPut("/departments/{id:int}", async (int id, DepartmentInput i, KpiDbContext db) =>
{
    var d = await db.Departments.FindAsync(id);
    if (d is null) return Results.NotFound();
    var name = i.Name.Trim();
    if (name.Length == 0) return Invalid("Name is required.");
    if (await db.Departments.AnyAsync(x => x.Name == name && x.Id != id)) return Invalid("A department with that name already exists.");
    d.Name = name; d.Description = i.Description;
    await db.SaveChangesAsync();
    return Results.Ok(new DepartmentDto(d.Id, d.Name, d.Description, await db.Kpis.CountAsync(k => k.DepartmentId == id)));
});

api.MapDelete("/departments/{id:int}", async (int id, KpiDbContext db) =>
{
    var d = await db.Departments.FindAsync(id);
    if (d is null) return Results.NotFound();
    if (await db.Kpis.AnyAsync(k => k.DepartmentId == id)) return Invalid("Move or delete this department's KPIs first.");
    db.Departments.Remove(d); await db.SaveChangesAsync();
    return Results.NoContent();
});

// ---- KPIs
api.MapGet("/kpis", (KpiQueries q, CancellationToken ct) => q.AllAsync(ct));

api.MapGet("/kpis/{id:int}", async (int id, KpiQueries q, CancellationToken ct) =>
    await q.DetailAsync(id, ct) is { } d ? Results.Ok(d) : Results.NotFound());

static void Apply(Kpi.Api.Domain.Kpi k, KpiInput i)
{
    k.Name = i.Name.Trim(); k.Description = i.Description; k.Unit = i.Unit?.Trim() ?? "";
    k.Direction = i.Direction; k.Frequency = i.Frequency; k.Target = i.Target;
    k.WarningThreshold = i.WarningThreshold; k.Owner = i.Owner; k.IsActive = i.IsActive; k.DepartmentId = i.DepartmentId;
}

api.MapPost("/kpis", async (KpiInput i, KpiDbContext db, KpiQueries q) =>
{
    if (Check(i) is { } err) return Invalid(err);
    if (!await db.Departments.AnyAsync(d => d.Id == i.DepartmentId)) return Invalid("Unknown department.");
    var k = new Kpi.Api.Domain.Kpi(); Apply(k, i);
    db.Kpis.Add(k); await db.SaveChangesAsync();
    return Results.Created($"/api/kpis/{k.Id}", await q.DetailAsync(k.Id));
});

api.MapPut("/kpis/{id:int}", async (int id, KpiInput i, KpiDbContext db, KpiQueries q) =>
{
    var k = await db.Kpis.FindAsync(id);
    if (k is null) return Results.NotFound();
    if (Check(i) is { } err) return Invalid(err);
    if (!await db.Departments.AnyAsync(d => d.Id == i.DepartmentId)) return Invalid("Unknown department.");
    Apply(k, i); await db.SaveChangesAsync();
    return Results.Ok(await q.DetailAsync(id));
});

api.MapDelete("/kpis/{id:int}", async (int id, KpiDbContext db) =>
{
    var k = await db.Kpis.FindAsync(id);
    if (k is null) return Results.NotFound();
    db.Kpis.Remove(k); await db.SaveChangesAsync();
    return Results.NoContent();
});

// ---- Entries (one reading per KPI per date; posting an existing date updates it)
api.MapPost("/kpis/{id:int}/entries", async (int id, EntryInput i, KpiDbContext db, KpiQueries q) =>
{
    if (!await db.Kpis.AnyAsync(k => k.Id == id)) return Results.NotFound();
    var e = await db.Entries.FirstOrDefaultAsync(x => x.KpiId == id && x.Date == i.Date);
    if (e is null) db.Entries.Add(new KpiEntry { KpiId = id, Date = i.Date, Value = i.Value, Note = i.Note });
    else { e.Value = i.Value; e.Note = i.Note; }
    await db.SaveChangesAsync();
    return Results.Ok(await q.DetailAsync(id));
});

api.MapDelete("/kpis/{id:int}/entries/{entryId:int}", async (int id, int entryId, KpiDbContext db, KpiQueries q) =>
{
    var e = await db.Entries.FirstOrDefaultAsync(x => x.Id == entryId && x.KpiId == id);
    if (e is null) return Results.NotFound();
    db.Entries.Remove(e); await db.SaveChangesAsync();
    return Results.Ok(await q.DetailAsync(id));
});

app.Run();
