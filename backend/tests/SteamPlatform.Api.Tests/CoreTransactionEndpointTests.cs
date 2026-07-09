using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Shared;

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
    [InlineData(HttpMethodName.Get, "/api/refunds")]
    public async Task Player_core_transaction_endpoints_require_authentication(HttpMethodName method, string url)
    {
        using var request = new HttpRequestMessage(method.ToHttpMethod(), url);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_refund_requires_authentication()
    {
        using var response = await _client.PostAsJsonAsync("/api/refunds", new
        {
            orderId = "O_DST_001",
            reason = "changed mind"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_refund_audit_requires_authentication()
    {
        using var response = await _client.PostAsJsonAsync("/api/admin/refunds/R001/approve", new
        {
            reason = "approved"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_cdkey_batch_requires_authentication()
    {
        using var response = await _client.PostAsJsonAsync("/api/developer/cdkey-batches", new
        {
            gameId = "GAME_DST",
            batchNo = "DST-DEMO",
            validFrom = DateTime.UtcNow,
            expireTime = DateTime.UtcNow.AddDays(30),
            quantity = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Redeem_cdkey_requires_authentication()
    {
        using var response = await _client.PostAsJsonAsync("/api/cdkeys/redeem", new
        {
            cdkey = "DST-TEST-0000"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Recharge_wallet_requires_authentication()
    {
        using var response = await _client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount = 10,
            idempotencyKey = "recharge-test"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Recharge_wallet_requires_player_role()
    {
        AuthorizeAs("ADMIN");

        using var response = await _client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount = 10,
            idempotencyKey = "recharge-test"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Recharge_wallet_validates_idempotency_key_before_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount = 10,
            idempotencyKey = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40001", body);
        Assert.Contains("IDEMPOTENCY_KEY_REQUIRED", body);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000)]
    [InlineData(1.001)]
    public async Task Recharge_wallet_validates_amount_before_opening_database(decimal amount)
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount,
            idempotencyKey = "recharge-test"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40901", body);
        Assert.Contains("INVALID_AMOUNT", body);
    }

    [Fact]
    public async Task Wallet_summary_returns_wallet_not_found_api_response()
    {
        using var customFactory = CreateFactoryWithCoreTransactionService(new StubCoreTransactionService
        {
            GetWallet = (_, _) => throw new ResourceNotFoundException("Wallet does not exist.")
        });
        using var client = customFactory.CreateClient();
        AuthorizeAsPlayer(client);

        using var response = await client.GetAsync("/api/wallet");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40401", body);
        Assert.Contains("WALLET_NOT_FOUND", body);
        Assert.Contains("\"data\":null", body);
    }

    [Fact]
    public async Task Wallet_transactions_returns_wallet_not_found_api_response()
    {
        using var customFactory = CreateFactoryWithCoreTransactionService(new StubCoreTransactionService
        {
            ListWalletTransactions = (_, _, _, _) => throw new ResourceNotFoundException("Wallet does not exist.")
        });
        using var client = customFactory.CreateClient();
        AuthorizeAsPlayer(client);

        using var response = await client.GetAsync("/api/wallet/transactions?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40401", body);
        Assert.Contains("WALLET_NOT_FOUND", body);
        Assert.Contains("\"data\":null", body);
    }

    [Fact]
    public async Task Recharge_wallet_returns_numeric_business_error_code()
    {
        using var customFactory = CreateFactoryWithCoreTransactionService(new StubCoreTransactionService
        {
            RechargeWallet = (_, _, _) => throw new BusinessRuleException("IDEMPOTENCY_CONFLICT", "IdempotencyKey is already used by another wallet transaction.")
        });
        using var client = customFactory.CreateClient();
        AuthorizeAsPlayer(client);

        using var response = await client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount = 10,
            idempotencyKey = "recharge-conflict"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40902", body);
        Assert.Contains("IDEMPOTENCY_CONFLICT", body);
        Assert.Contains("IdempotencyKey is already used by another wallet transaction.", body);
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
    public async Task Create_refund_validates_required_fields_before_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/refunds", new
        {
            orderId = "",
            reason = "changed mind"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("OrderId and Reason are required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_cdkey_batch_validates_quantity_before_opening_database()
    {
        AuthorizeAs("DEVELOPER");

        using var response = await _client.PostAsJsonAsync("/api/developer/cdkey-batches", new
        {
            gameId = "GAME_DST",
            batchNo = "DST-DEMO",
            validFrom = DateTime.UtcNow,
            expireTime = DateTime.UtcNow.AddDays(30),
            quantity = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Quantity must be greater than 0.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Redeem_cdkey_validates_required_fields_before_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/cdkeys/redeem", new
        {
            cdkey = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Cdkey is required.", await response.Content.ReadAsStringAsync());
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
        AuthorizeAs(_client, "PLAYER");
    }

    private void AuthorizeAsPlayer(HttpClient client)
    {
        AuthorizeAs(client, "PLAYER");
    }

    private void AuthorizeAs(string role)
    {
        AuthorizeAs(_client, role);
    }

    private void AuthorizeAs(HttpClient client, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var principalId = role.ToUpperInvariant() switch
        {
            "PLAYER" => "P001",
            "ADMIN" => "ADM001",
            _ => "DEV001"
        };
        var token = auth.CreateToken(new AuthClaims(role, principalId, "tester", DateTimeOffset.UtcNow.AddMinutes(10)));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private WebApplicationFactory<Program> CreateFactoryWithCoreTransactionService(ICoreTransactionService service) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICoreTransactionService>();
                services.AddSingleton(service);
            });
        });

    public enum HttpMethodName
    {
        Get
    }
}

internal sealed class StubCoreTransactionService : ICoreTransactionService
{
    public Func<AuthClaims, CancellationToken, Task<WalletSummary>>? GetWallet { get; init; }
    public Func<AuthClaims, RechargeWalletRequest, CancellationToken, Task<RechargeWalletResult>>? RechargeWallet { get; init; }
    public Func<AuthClaims, int, int, CancellationToken, Task<PagedResponse<WalletTransactionEntry>>>? ListWalletTransactions { get; init; }

    public Task<WalletSummary> GetWalletAsync(AuthClaims claims, CancellationToken cancellationToken) =>
        GetWallet?.Invoke(claims, cancellationToken) ?? throw new NotImplementedException();

    public Task<RechargeWalletResult> RechargeWalletAsync(AuthClaims claims, RechargeWalletRequest request, CancellationToken cancellationToken) =>
        RechargeWallet?.Invoke(claims, request, cancellationToken) ?? throw new NotImplementedException();

    public Task<PagedResponse<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken) =>
        ListWalletTransactions?.Invoke(claims, page, pageSize, cancellationToken) ?? throw new NotImplementedException();

    public Task<OrderSummary> BuyGameAsync(AuthClaims claims, CreateOrderRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<OrderSummary> ClaimFreeGameAsync(AuthClaims claims, string gameId, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(AuthClaims claims, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<OrderSummary> GetOrderAsync(AuthClaims claims, string orderId, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<LibraryEntry>> ListLibraryAsync(AuthClaims claims, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<LibraryEntry> AddPlaytimeAsync(AuthClaims claims, string gameId, UpdatePlaytimeRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<RefundSummary> CreateRefundAsync(AuthClaims claims, CreateRefundRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<RefundSummary>> ListRefundsAsync(AuthClaims claims, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<RefundSummary>> ListAllRefundsAsync(AuthClaims claims, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<RefundSummary> ApproveRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<RefundSummary> RejectRefundAsync(AuthClaims claims, string refundId, AuditRefundRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<CdkeyBatchSummary> CreateCdkeyBatchAsync(AuthClaims claims, CreateCdkeyBatchRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<CdkeyRedeemResult> RedeemCdkeyAsync(AuthClaims claims, RedeemCdkeyRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
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
