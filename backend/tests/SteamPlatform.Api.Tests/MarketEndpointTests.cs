using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class MarketEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_orders_requires_player_authentication_before_opening_database()
    {
        using var response = await _client.GetAsync("/api/market/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_order_requires_player_authentication_before_input_validation()
    {
        using var response = await _client.PostAsJsonAsync("/api/market/orders", new
        {
            orderType = "BUY",
            templateId = " ",
            itemId = (string?)null,
            targetPrice = 0
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_order_forbids_admin_tokens_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/market/orders/MO001/cancel")
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("SUPER_ADMIN", "ADM001", "rootadmin"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Match_requires_player_authentication_before_opening_database()
    {
        using var response = await _client.PostAsJsonAsync("/api/market/match", new
        {
            templateId = "ITPL_CS2_AK_REDLINE"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string CreateToken(string role, string principalId, string account)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return auth.CreateToken(new AuthClaims(role, principalId, account, DateTimeOffset.UtcNow.AddMinutes(10)));
    }
}
