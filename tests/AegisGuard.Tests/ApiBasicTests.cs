using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AegisGuard.Tests;

public record SecurityLog(string source, string severity, string message, string? metadata, DateTime? timestamp);
public record LogStat(string severity, int count);

public class ApiBasicTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ApiBasicTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_200()
    {
        var resp = await _client.GetAsync("/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metrics_returns_text()
    {
        var resp = await _client.GetAsync("/metrics");
        resp.IsSuccessStatusCode.Should().BeTrue();
        (resp.Content.Headers.ContentType?.MediaType ?? "").Should().Contain("text");
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("#"); // Prometheus Exposition Format
    }

    [Fact]
    public async Task Logs_post_and_stats_aggregate()
    {
        var before = await _client.GetFromJsonAsync<List<SecurityLog>>("/api/logs") ?? new();

        var l1 = new SecurityLog("zap","Warning","XSS gefunden",@"{""url"":""/t1""}",null);
        var l2 = new SecurityLog("trivy","Critical","CVEs gefunden",@"{""image"":""app:1.0""}",null);

        (await _client.PostAsJsonAsync("/api/logs", l1)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/logs", l2)).EnsureSuccessStatusCode();

        var after = await _client.GetFromJsonAsync<List<SecurityLog>>("/api/logs") ?? new();
        after.Count.Should().Be(before.Count + 2);

        var stats = await _client.GetFromJsonAsync<List<LogStat>>("/api/logs/stats") ?? new();
        stats.Should().Contain(s => s.severity == "Warning" && s.count >= 1);
        stats.Should().Contain(s => s.severity == "Critical" && s.count >= 1);
    }
}
