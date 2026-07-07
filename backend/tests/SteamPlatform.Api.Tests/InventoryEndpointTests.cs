using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class InventoryEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Inventory_list_requires_player_authentication()
    {
        using var response = await _client.GetAsync("/api/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inventory_drop_requires_player_authentication_before_input_validation()
    {
        using var response = await _client.PostAsJsonAsync("/api/inventory/drop", new
        {
            gameId = " "
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Inventory_drop_validates_game_id_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/inventory/drop")
        {
            Content = JsonContent.Create(new
            {
                gameId = " "
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("PLAYER", "P001", "alice"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("GameId is required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Inventory_list_forbids_non_player_tokens_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/inventory");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("SUPER_ADMIN", "ADM001", "rootadmin"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Item_transfers_require_player_authentication()
    {
        using var response = await _client.GetAsync("/api/items/ITEM001/transfers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Item_transfers_forbid_non_player_tokens_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/items/ITEM001/transfers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("SUPER_ADMIN", "ADM001", "rootadmin"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private string CreateToken(string role, string principalId, string account)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return auth.CreateToken(new AuthClaims(role, principalId, account, DateTimeOffset.UtcNow.AddMinutes(10)));
    }
}
