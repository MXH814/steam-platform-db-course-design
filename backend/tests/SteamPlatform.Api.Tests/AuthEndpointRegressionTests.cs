using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class AuthEndpointRegressionTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [MemberData(nameof(InvalidLoginRequests))]
    public async Task Login_rejects_null_and_whitespace_fields_before_database(object request)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Role, Account and Password are required.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("SUPER_ADMIN")]
    [InlineData("AUDITOR")]
    [InlineData("UNKNOWN")]
    public async Task Login_rejects_internal_or_unknown_role_names(string role)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            role,
            account = "rootadmin",
            password = "admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Role must be PLAYER, DEVELOPER or ADMIN.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(InvalidRegistrationRequests))]
    public async Task Registration_aliases_reject_null_and_whitespace_fields(string uri, object request)
    {
        using var response = await _client.PostAsJsonAsync(uri, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Account, Password and Nickname are required.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("SUPER_ADMIN")]
    [InlineData("AUDITOR")]
    [InlineData("RISK_ADMIN")]
    [InlineData("CUSTOMER_SERVICE")]
    public async Task Me_accepts_supported_admin_claim_roles(string role)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(role, await response.Content.ReadAsStringAsync());
    }

    public static TheoryData<object> InvalidLoginRequests() =>
        new()
        {
            new { role = (string?)null, account = "rootadmin", password = "admin" },
            new { role = " ", account = "rootadmin", password = "admin" },
            new { role = "ADMIN", account = (string?)null, password = "admin" },
            new { role = "ADMIN", account = " ", password = "admin" },
            new { role = "ADMIN", account = "rootadmin", password = (string?)null },
            new { role = "ADMIN", account = "rootadmin", password = " " }
        };

    public static TheoryData<string, object> InvalidRegistrationRequests()
    {
        var data = new TheoryData<string, object>();
        foreach (var uri in new[] { "/api/auth/register", "/api/auth/register/player" })
        {
            data.Add(uri, new { account = (string?)null, password = "password", nickname = "Player" });
            data.Add(uri, new { account = " ", password = "password", nickname = "Player" });
            data.Add(uri, new { account = "player", password = (string?)null, nickname = "Player" });
            data.Add(uri, new { account = "player", password = " ", nickname = "Player" });
            data.Add(uri, new { account = "player", password = "password", nickname = (string?)null });
            data.Add(uri, new { account = "player", password = "password", nickname = " " });
        }

        return data;
    }

    private string CreateToken(string role)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        return auth.CreateToken(new AuthClaims(role, "ADM001", "rootadmin", DateTimeOffset.UtcNow.AddMinutes(10)));
    }
}
