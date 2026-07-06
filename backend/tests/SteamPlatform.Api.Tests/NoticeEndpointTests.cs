using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Api.Features.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class NoticeEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_notice_endpoints_require_authentication_before_input_validation()
    {
        using var response = await _client.PostAsJsonAsync("/api/admin/notices", new
        {
            title = " ",
            content = "content",
            priority = 1,
            expireTime = (DateTime?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_notice_endpoints_forbid_player_tokens_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/notices")
        {
            Content = JsonContent.Create(new
            {
                title = "notice",
                content = "content",
                priority = 1,
                expireTime = (DateTime?)null
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("PLAYER", "P001", "alice"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_notice_endpoints_validate_input_before_opening_database()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/notices")
        {
            Content = JsonContent.Create(new
            {
                title = " ",
                content = "content",
                priority = 1,
                expireTime = (DateTime?)null
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("SUPER_ADMIN", "ADM001", "rootadmin"));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Title and Content are required.", await response.Content.ReadAsStringAsync());
    }

    private string CreateToken(string role, string principalId, string account)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return auth.CreateToken(new AuthClaims(role, principalId, account, DateTimeOffset.UtcNow.AddMinutes(10)));
    }
}
