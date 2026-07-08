using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class CommunityEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_review_requires_player_authentication_before_input_validation()
    {
        using var response = await _client.PostAsJsonAsync("/api/games/G001/reviews", new
        {
            isRecommend = true,
            content = " "
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_review_forbids_admin_tokens_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/games/G001/reviews")
        {
            Content = JsonContent.Create(new
            {
                isRecommend = true,
                content = "Good game."
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("SUPER_ADMIN", "ADM001", "rootadmin"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_review_validates_content_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/games/G001/reviews")
        {
            Content = JsonContent.Create(new
            {
                isRecommend = true,
                content = " "
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("PLAYER", "P001", "alice"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("GameId and Content are required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Update_review_requires_player_authentication_before_input_validation()
    {
        using var response = await _client.PutAsJsonAsync("/api/reviews/REV001", new
        {
            isRecommend = true,
            content = " "
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_review_validates_content_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/reviews/REV001")
        {
            Content = JsonContent.Create(new
            {
                isRecommend = false,
                content = " "
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("PLAYER", "P001", "alice"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("ReviewId and Content are required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unlock_achievement_requires_player_authentication_before_opening_database()
    {
        using var response = await _client.PostAsJsonAsync("/api/achievements/ACH001/unlock", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unlock_achievement_forbids_admin_tokens_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/achievements/ACH001/unlock")
        {
            Content = JsonContent.Create(new { })
        };
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