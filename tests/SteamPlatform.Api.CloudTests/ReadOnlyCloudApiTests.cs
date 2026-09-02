using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace SteamPlatform.Api.CloudTests;

public sealed class ReadOnlyCloudApiTests(ITestOutputHelper output)
{
    private const string BaseUrlEnvironmentVariable = "STEAM_API_TEST_BASE_URL";

    [Fact]
    public async Task Health_endpoints_confirm_api_and_oracle_are_available()
    {
        using var client = CreateClientOrSkip();
        if (client is null)
        {
            return;
        }

        using var serviceResponse = await client.GetAsync("health");
        using var databaseResponse = await client.GetAsync("health/database");

        await AssertSuccessAsync(serviceResponse);
        await AssertSuccessAsync(databaseResponse);

        using var serviceJson = JsonDocument.Parse(await serviceResponse.Content.ReadAsStringAsync());
        using var databaseJson = JsonDocument.Parse(await databaseResponse.Content.ReadAsStringAsync());
        Assert.Equal("OK", serviceJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("OK", databaseJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("Oracle", databaseJson.RootElement.GetProperty("database").GetString());
    }

    [Theory]
    [InlineData("PLAYER", "alice", "alice", "PLAYER")]
    [InlineData("PLAYER", "bob", "bob", "PLAYER")]
    [InlineData("ADMIN", "rootadmin", "admin", "SUPER_ADMIN")]
    [InlineData("DEVELOPER", "valve@example.com", "valve", "DEVELOPER")]
    [InlineData("DEVELOPER", "klei@example.com", "klei", "DEVELOPER")]
    public async Task Documented_demo_accounts_can_login_and_read_current_identity(
        string requestedRole,
        string account,
        string password,
        string expectedRole)
    {
        using var client = CreateClientOrSkip();
        if (client is null)
        {
            return;
        }

        var token = await LoginAsync(client, requestedRole, account, password);
        using var request = AuthorizedRequest(HttpMethod.Get, "api/auth/me", token);
        using var response = await client.SendAsync(request);

        await AssertSuccessAsync(response);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedRole, json.RootElement.GetProperty("role").GetString());
        Assert.Equal(account, json.RootElement.GetProperty("account").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("principalId").GetString()));
    }

    [Fact]
    public async Task Public_catalog_and_community_reads_use_current_routes()
    {
        using var client = CreateClientOrSkip();
        if (client is null)
        {
            return;
        }

        string[] endpoints =
        [
            "api/notices",
            "api/games?pageSize=5",
            "api/games/GAME_CS2",
            "api/games/GAME_DST/reviews",
            "api/item-templates?gameId=GAME_CS2",
            "api/market?gameId=GAME_CS2",
            "api/market/trades"
        ];

        foreach (var endpoint in endpoints)
        {
            using var response = await client.GetAsync(endpoint);
            await AssertSuccessAsync(response, endpoint);
        }
    }

    [Fact]
    public async Task Player_read_endpoints_use_current_routes_without_mutating_cloud_data()
    {
        using var client = CreateClientOrSkip();
        if (client is null)
        {
            return;
        }

        var token = await LoginAsync(client, "PLAYER", "alice", "alice");
        string[] endpoints =
        [
            "api/wallet",
            "api/wallet/transactions?page=1&pageSize=5",
            "api/orders",
            "api/library",
            "api/refunds",
            "api/inventory?gameId=GAME_CS2",
            "api/games/GAME_DST/friends-who-play",
            "api/market/orders",
            "api/games/GAME_DST/achievements"
        ];

        foreach (var endpoint in endpoints)
        {
            using var request = AuthorizedRequest(HttpMethod.Get, endpoint, token);
            using var response = await client.SendAsync(request);
            await AssertSuccessAsync(response, endpoint);
        }
    }

    [Fact]
    public async Task Admin_can_read_refund_queue_without_mutating_cloud_data()
    {
        using var client = CreateClientOrSkip();
        if (client is null)
        {
            return;
        }

        var token = await LoginAsync(client, "ADMIN", "rootadmin", "admin");
        using var request = AuthorizedRequest(HttpMethod.Get, "api/admin/refunds", token);
        using var response = await client.SendAsync(request);

        await AssertSuccessAsync(response);
    }

    private HttpClient? CreateClientOrSkip()
    {
        var configured = Environment.GetEnvironmentVariable(BaseUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            output.WriteLine($"{BaseUrlEnvironmentVariable} is not set; skipping optional cloud API checks.");
            return null;
        }

        if (!Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException($"{BaseUrlEnvironmentVariable} must be an absolute HTTP(S) URL.");
        }

        if (baseUri.AbsolutePath.Trim('/').Length > 0)
        {
            throw new InvalidOperationException(
                $"{BaseUrlEnvironmentVariable} must contain only the API host. Do not append /api.");
        }

        return new HttpClient
        {
            BaseAddress = new Uri(baseUri.GetLeftPart(UriPartial.Authority) + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static async Task<string> LoginAsync(
        HttpClient client,
        string role,
        string account,
        string password)
    {
        using var response = await client.PostAsJsonAsync("api/auth/login", new { role, account, password });
        await AssertSuccessAsync(response, $"login {role}/{account}");

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private static HttpRequestMessage AuthorizedRequest(HttpMethod method, string endpoint, string token)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task AssertSuccessAsync(HttpResponseMessage response, string? operation = null)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail(
            $"{operation ?? response.RequestMessage?.RequestUri?.ToString()} returned {(int)response.StatusCode}: {body}");
    }
}
