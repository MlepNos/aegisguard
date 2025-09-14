// tests/AegisGuard.Api.Tests/HealthTests.cs
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HealthTests(WebApplicationFactory<Program> f) => _factory = f;

    [Fact]
    public async Task Health_returns_200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
