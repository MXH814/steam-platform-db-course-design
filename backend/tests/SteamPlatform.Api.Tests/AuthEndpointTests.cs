using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class AuthEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Me_requires_authentication()
    {
        using var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_current_claims_without_opening_database()
    {
        var token = CreateToken("PLAYER", "P001", "alice");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal("PLAYER", json["role"]!.GetValue<string>());
        Assert.Equal("P001", json["principalId"]!.GetValue<string>());
        Assert.Equal("alice", json["account"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("", "alice", "alice", "Role, Account and Password are required.")]
    [InlineData("PLAYER", "", "alice", "Role, Account and Password are required.")]
    [InlineData("PLAYER", "alice", "", "Role, Account and Password are required.")]
    public async Task Login_validates_required_fields_before_opening_database(
        string role,
        string account,
        string password,
        string expectedMessage)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new { role, account, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_rejects_unknown_role_before_opening_database()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            role = "ROOT",
            account = "root",
            password = "secret"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Role must be PLAYER, DEVELOPER or ADMIN.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("", "password", "nickname")]
    [InlineData("player", "", "nickname")]
    [InlineData("player", "password", "")]
    public async Task Register_validates_required_fields_before_opening_database(string account, string password, string nickname)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register", new { account, password, nickname });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Account, Password and Nickname are required.", await response.Content.ReadAsStringAsync());
    }

    private string CreateToken(string role, string principalId, string account)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return auth.CreateToken(new AuthClaims(role, principalId, account, DateTimeOffset.UtcNow.AddMinutes(10)));
    }
}
