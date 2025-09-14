using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using AegisGuard.Infrastructure;
using Microsoft.EntityFrameworkCore;
using AegisGuard.Domain;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
             ?? builder.Configuration.GetConnectionString("Default")
             ?? "Host=localhost;Port=5432;Database=aegisguard;Username=aegis;Password=aegis";



builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr, b => b.MigrationsAssembly("AegisGuard.Api"))
);
/*
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .WithOrigins(
        "http://localhost:5000", "https://localhost:5001",  // häufige Dev-Ports
        "http://localhost:5242", "https://localhost:7242"   // typischer Blazor-Port
     )
));*/


builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowAnyOrigin()   // <— für lokale Entwicklung am einfachsten
));
/*
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("AegisGuard.Api")   // <— Migrationen ins API-Projekt
    )
);


builder.Logging.AddConsole();
builder.Services.AddHealthChecks()
    .AddNpgSql(connStr, name: "postgres");
*/

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpMetrics(); // Prometheus request metrics


app.UseCors("frontend");

// Health + Metrics
app.MapHealthChecks("/health", new HealthCheckOptions());
app.MapMetrics(); // /metrics


app.MapGet("/api/analytics/severity-by-day", async (AppDbContext db) =>
{
    var since = DateTime.UtcNow.Date.AddDays(-30);

    var rows = await db.SecurityLogs
        .AsNoTracking()
        .Where(x => x.Timestamp >= since)
        .GroupBy(x => new { day = x.Timestamp.Date, x.Severity })
        .Select(g => new
        {
            g.Key.day,
            g.Key.Severity,
            Count = g.Count()
        })
        .OrderBy(r => r.day)
        .ThenBy(r => r.Severity)
        .ToListAsync();

    return Results.Ok(rows);
});

app.MapGet("/api/analytics/top-sources", async (AppDbContext db, int limit = 10) =>
{
    var rows = await db.SecurityLogs
        .GroupBy(x => x.Source!)
        .Select(g => new { name = g.Key, count = g.Count() })
        .OrderByDescending(x => x.count)
        .Take(limit)
        .ToListAsync();
    return Results.Ok(rows);
});

app.MapGet("/api/analytics/severity-share", async (AppDbContext db) =>
{
    var rows = await db.SecurityLogs
        .GroupBy(x => x.Severity!)
        .Select(g => new { severity = g.Key, count = g.Count() })
        .ToListAsync();
    return Results.Ok(rows);
});

// /api/analytics/hourly-heatmap
app.MapGet("/api/analytics/hourly-heatmap", async (AppDbContext db) =>
{
    var since = DateTime.UtcNow.AddDays(-7);

    var rows = await db.SecurityLogs
        .Where(x => x.Timestamp >= since)
        .Select(x => new
        {
            // .DayOfWeek: 0..6 (So..Sa)
            day  = (int)x.Timestamp.DayOfWeek,
            hour = x.Timestamp.Hour
        })
        .GroupBy(x => new { x.day, x.hour })
        .Select(g => new { g.Key.day, g.Key.hour, value = g.Count() })
        .OrderBy(x => x.day).ThenBy(x => x.hour)
        .ToListAsync();

    return Results.Ok(rows);
});

// InMemory-Store (Schritt 1: noch ohne DB)
// var logs = new List<SecurityLog>();



/*
// Endpoints
app.MapPost("/api/logs", (SecurityLog log) =>
{
    var newLog = log with { Timestamp = DateTime.UtcNow };
    logs.Add(newLog);
    return Results.Created($"/api/logs/{logs.Count - 1}", newLog);
})
.WithName("IngestLog")
.WithOpenApi();

app.MapGet("/api/logs", () => logs)
.WithName("GetLogs")
.WithOpenApi();

app.MapGet("/api/logs/stats", () =>
{
    var stats = logs
        .GroupBy(l => l.Severity)
        .Select(g => new { severity = g.Key, count = g.Count() })
        .OrderByDescending(x => x.count);
    return Results.Ok(stats);
})
.WithName("GetLogStats")
.WithOpenApi();
*/

// POST: Log anlegen (DB)
app.MapPost("/api/logs", async (SecurityLogCreateDto input, AppDbContext db) =>
{
    var entity = new SecurityLog
    {
        Source    = input.Source,
        Severity  = input.Severity,
        Message   = input.Message,
        Metadata  = input.Metadata,
        Timestamp = DateTime.UtcNow
    };

    db.SecurityLogs.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/api/logs/{entity.Id}", entity);
})
.WithName("IngestLog")
.WithOpenApi();

// GET: Alle Logs (neueste zuerst)
app.MapGet("/api/logs", async (AppDbContext db) =>
    await db.SecurityLogs
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync())
.WithName("GetLogs")
.WithOpenApi();

// GET: Stats (Counts pro Severity)
app.MapGet("/api/logs/stats", async (AppDbContext db) =>
{
    var stats = await db.SecurityLogs
        .GroupBy(l => l.Severity)
        .Select(g => new { severity = g.Key, count = g.Count() })
        .OrderByDescending(x => x.count)
        .ToListAsync();

    return Results.Ok(stats);
})
.WithName("GetLogStats")
.WithOpenApi();


app.Run();
public record SecurityLogCreateDto(string Source, string Severity, string Message, string? Metadata);

// Datentyp
//public record SecurityLog(string Source, string Severity, string Message, string? Metadata, DateTime? Timestamp);
public partial class Program { } // für WebApplicationFactory in Integrationstests
