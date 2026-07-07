using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(HttpMethodName.Get, "/api/wallet")]
    [InlineData(HttpMethodName.Get, "/api/wallet/transactions")]
    [InlineData(HttpMethodName.Get, "/api/orders")]
    [InlineData(HttpMethodName.Get, "/api/library")]
    public async Task Player_core_transaction_endpoints_require_authentication(HttpMethodName method, string url)
    {
        using var request = new HttpRequestMessage(method.ToHttpMethod(), url);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_order_validates_required_fields_before_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            gameId = "",
            idempotencyKey = "idem-test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("GameId and IdempotencyKey are required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Add_playtime_validates_positive_minutes_before_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/library/GAME_DST/playtime", new
        {
            minutesToAdd = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("MinutesToAdd must be greater than 0.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Free_claim_rejects_non_cs2_without_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsync("/api/games/GAME_DST/free-claim", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("GAME_NOT_FREE", await response.Content.ReadAsStringAsync());
    }

    private void AuthorizeAsPlayer()
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var token = auth.CreateToken(new AuthClaims("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(10)));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public enum HttpMethodName
    {
        Get
    }
}

internal static class HttpMethodNameExtensions
{
    public static HttpMethod ToHttpMethod(this CoreTransactionEndpointTests.HttpMethodName method) =>
        method switch
        {
            CoreTransactionEndpointTests.HttpMethodName.Get => HttpMethod.Get,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
}
