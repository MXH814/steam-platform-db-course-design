using System.Net;
using System.Net.Http.Json;

namespace SteamPlatform.Api.Tests;

public sealed class ProtectedEndpointRegressionTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [MemberData(nameof(ProtectedWriteRequests))]
    public async Task Protected_write_endpoints_authenticate_before_validating_input(string uri, object body)
    {
        using var response = await _client.PostAsJsonAsync(uri, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, object> ProtectedWriteRequests() =>
        new()
        {
            { "/api/admin/notices", new { title = "", content = "", priority = 0 } },
            { "/api/orders", new { gameId = "", idempotencyKey = "" } },
            { "/api/wallet/recharge", new { amount = -1m, idempotencyKey = "" } },
            { "/api/inventory/drop", new { gameId = "" } },
            { "/api/market/orders", new { orderType = "", templateId = "", targetPrice = 0m } },
            { "/api/games/GAME_DST/reviews", new { content = "", rating = 0 } },
            { "/api/achievements/ACH_DST_FIRST_DAY/unlock", new { } },
            { "/api/developer/cdkey-batches", new { gameId = "", batchNo = "", quantity = 0 } },
            { "/api/cdkeys/redeem", new { cdkey = "" } }
        };
}
