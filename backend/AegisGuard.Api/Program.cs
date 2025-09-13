using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;

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
var logs = new List<SecurityLog>();

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

app.Run();

// Datentyp
public record SecurityLog(string Source, string Severity, string Message, string? Metadata, DateTime? Timestamp);
