using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class SocialEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/friends")]
    [InlineData("/api/games/GAME_DST/friends-who-play")]
    [InlineData("/api/notifications")]
    public async Task Player_social_reads_require_authentication(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Message_write_requires_authentication_before_input_validation()
    {
        using var response = await _client.PostAsJsonAsync("/api/friends/P002/messages", new { content = " " });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Message_write_forbids_admin_tokens_before_database_access()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/friends/P002/messages")
        {
            Content = JsonContent.Create(new { content = "hello" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("SUPER_ADMIN", "ADM001", "rootadmin"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Message_write_validates_content_before_database_access()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/friends/P002/messages")
        {
            Content = JsonContent.Create(new { content = " " })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("PLAYER", "P001", "alice"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("FriendUserId and Content are required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Workshop_subscription_requires_player_role()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/workshop/WS001/subscription")
        {
            Content = JsonContent.Create(new { isSubscribed = true })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("DEVELOPER", "DEV001", "studio"));

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
