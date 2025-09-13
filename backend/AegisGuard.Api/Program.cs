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

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("AegisGuard.Api")   // <— Migrationen ins API-Projekt
    )
);


var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpMetrics(); // Prometheus request metrics


app.UseCors("frontend");

// Health + Metrics
app.MapHealthChecks("/health", new HealthCheckOptions());
app.MapMetrics(); // /metrics

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
