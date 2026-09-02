using System.Net;
using System.Net.Http.Json;
using SteamPlatform.Application.Diagnostics;

namespace SteamPlatform.Api.Tests;

public sealed class HealthEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    [Theory]
    [InlineData("/health/database")]
    [InlineData("/api/health/database")]
    public async Task Database_health_is_available_on_operational_and_frontend_routes(string route)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<DatabaseHealthResult>();
        Assert.Equal("SKIPPED", health?.Status);
    }
}
