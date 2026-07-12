using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionEndpointTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData(HttpMethodName.Get, "/api/wallet")]
    [InlineData(HttpMethodName.Get, "/api/wallet/transactions")]
    [InlineData(HttpMethodName.Get, "/api/wallet/history")]
    [InlineData(HttpMethodName.Get, "/api/wallet/history/WALLET-WT_TEST")]
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
    public async Task Recharge_wallet_requires_payment_method_before_opening_database()
    {
        AuthorizeAsPlayer();

        using var response = await _client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount = 10,
            idempotencyKey = "recharge-test"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40902", body);
        Assert.Contains("INVALID_PAYMENT_METHOD", body);
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
    public async Task Wallet_history_detail_returns_api_response()
    {
        using var customFactory = CreateFactoryWithCoreTransactionService(new StubCoreTransactionService
        {
            GetWalletHistoryEntry = (_, historyId, _) => Task.FromResult(new WalletHistoryEntry(
                historyId,
                "RECHARGE",
                DateTime.UtcNow,
                "Steam 钱包充值",
                PaymentMethods.WechatPay,
                30m,
                0m,
                0m,
                30m,
                30m,
                130m,
                null,
                null,
                null,
                "WT_TEST"))
        });
        using var client = customFactory.CreateClient();
        AuthorizeAsPlayer(client);

        using var response = await client.GetAsync("/api/wallet/history/WALLET-WT_TEST");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":0", body);
        Assert.Contains("\"historyId\":\"WALLET-WT_TEST\"", body);
        Assert.Contains("\"sourceType\":\"RECHARGE\"", body);
    }

    [Fact]
    public async Task Wallet_history_detail_missing_entry_returns_history_not_found_api_response()
    {
        using var customFactory = CreateFactoryWithCoreTransactionService(new StubCoreTransactionService
        {
            GetWalletHistoryEntry = (_, _, _) => throw new ResourceNotFoundException("Wallet history entry does not exist.")
        });
        using var client = customFactory.CreateClient();
        AuthorizeAsPlayer(client);

        using var response = await client.GetAsync("/api/wallet/history/ORDER-NOT_FOUND");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":40402", body);
        Assert.Contains("WALLET_HISTORY_NOT_FOUND", body);
        Assert.DoesNotContain("WALLET_NOT_FOUND", body);
    }

    [Fact]
    public async Task Wallet_history_contains_recharge_after_successful_recharge()
    {
        AuthorizeAsPlayer();
        var idempotencyKey = $"recharge-history-{Guid.NewGuid():N}";

        using var rechargeResponse = await _client.PostAsJsonAsync("/api/wallet/recharge", new
        {
            amount = 30,
            paymentMethod = PaymentMethods.WechatPay,
            idempotencyKey
        });

        Assert.Equal(HttpStatusCode.OK, rechargeResponse.StatusCode);

        using var historyResponse = await _client.GetAsync("/api/wallet/history?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var body = await historyResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"sourceType\":\"RECHARGE\"", body);
        Assert.Contains("\"paymentMethod\":\"WECHAT_PAY\"", body);
    }

    [Fact]
    public async Task Wallet_history_includes_library_entry_without_order_history()
    {
        AuthorizeAsPlayer(_client, "P002");

        using var response = await _client.GetAsync("/api/wallet/history?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"sourceType\":\"CDKEY_REDEEM\"", body);
        Assert.Contains("Don", body);
    }

    [Fact]
    public async Task Wallet_history_does_not_duplicate_ordered_library_entries()
    {
        AuthorizeAsPlayer();

        using var response = await _client.GetAsync("/api/wallet/history?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"sourceType\":\"BUY_GAME\"", body);
        Assert.DoesNotContain("\"historyId\":\"LIBRARY-P001-GAME_DST\"", body);
    }

    [Fact]
    public async Task External_payment_refund_approval_does_not_credit_wallet()
    {
        using var customFactory = CreateFactoryWithCoreTransactionService(new InMemoryCoreTransactionService());
        using var client = customFactory.CreateClient();
        AuthorizeAsPlayer(client, "P002");

        var walletBefore = await client.GetFromJsonAsync<ApiResponse<WalletSummary>>("/api/wallet");

        using var orderResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            gameId = "GAME_EXTERNAL_REFUND",
            idempotencyKey = $"external-refund-{Guid.NewGuid():N}",
            paymentMethod = PaymentMethods.Alipay
        });

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderSummary>();
        Assert.NotNull(order);

        using var refundResponse = await client.PostAsJsonAsync("/api/refunds", new
        {
            orderId = order!.OrderId,
            reason = "external refund test"
        });

        Assert.Equal(HttpStatusCode.Created, refundResponse.StatusCode);
        var refund = await refundResponse.Content.ReadFromJsonAsync<RefundSummary>();
        Assert.NotNull(refund);

        AuthorizeAs(client, "ADMIN");
        using var approveResponse = await client.PostAsJsonAsync($"/api/admin/refunds/{refund!.RefundId}/approve", new
        {
            reason = "approved"
        });

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        AuthorizeAsPlayer(client, "P002");
        var walletAfter = await client.GetFromJsonAsync<ApiResponse<WalletSummary>>("/api/wallet");

        Assert.NotNull(walletBefore);
        Assert.NotNull(walletAfter);
        Assert.NotNull(walletBefore!.Data);
        Assert.NotNull(walletAfter!.Data);
        Assert.Equal(walletBefore.Data!.AvailableBalance, walletAfter.Data!.AvailableBalance);

        using var historyResponse = await client.GetAsync("/api/wallet/history?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var historyBody = await historyResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"sourceType\":\"REFUND\"", historyBody);
        Assert.Contains("\"paymentMethod\":\"ALIPAY\"", historyBody);
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
    public async Task RedeemCdkey_handles_concurrent_requests_gracefully()
    {
        // Arrange: Create a batch with one CDKey as a developer
        using var factory = new SteamPlatformApiFactory();
        var devClient = factory.CreateClient();
        AuthorizeAs(devClient, "DEVELOPER");

        var batchRequest = new CreateCdkeyBatchRequest(
            "GAME_DST",
            $"CONCURRENCY-TEST-{Guid.NewGuid():N}",
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(1),
            1);
        using var batchResponse = await devClient.PostAsJsonAsync("/api/developer/cdkey-batches", batchRequest);
        batchResponse.EnsureSuccessStatusCode();
        var batchResult = await batchResponse.Content.ReadFromJsonAsync<CdkeyBatchSummary>();
        var cdkeyToRedeem = batchResult!.PlaintextKeys.Single();

        // Act: Two different players try to redeem the same key concurrently by calling the service directly
        var redeemRequest = new RedeemCdkeyRequest(cdkeyToRedeem);

        async Task<CdkeyRedeemResult> RedeemAsPlayer(string principalId)
        {
            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ICoreTransactionService>();
            var claims = new AuthClaims("PLAYER", principalId, principalId, DateTimeOffset.MaxValue);
            return await service.RedeemCdkeyAsync(claims, redeemRequest, CancellationToken.None);
        }

        var task1 = Task.Run(() => RedeemAsPlayer("P999"));
        var task2 = Task.Run(() => RedeemAsPlayer("P998"));

        var results = await Task.WhenAll(task1, task2);
        var result1 = results[0];
        var result2 = results[1];

        // Assert: One succeeds, one fails with a specific business error
        var successResult = string.Equals(result1.Result, "SUCCESS", StringComparison.Ordinal) ? result1 : result2;
        var failedResult = string.Equals(result1.Result, "SUCCESS", StringComparison.Ordinal) ? result2 : result1;

        Assert.Equal("SUCCESS", successResult.Result);
        Assert.Equal("REDEEMED", failedResult.Result);
        Assert.Contains("redeemed", failedResult.Message, StringComparison.OrdinalIgnoreCase);
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

    private void AuthorizeAsPlayer(HttpClient client, string principalId)
    {
        AuthorizeAs(client, "PLAYER", principalId);
    }

    private void AuthorizeAs(string role)
    {
        AuthorizeAs(_client, role);
    }

    private void AuthorizeAs(HttpClient client, string role)
    {
        var principalId = role.ToUpperInvariant() switch
        {
            "PLAYER" => "P001",
            "ADMIN" => "ADM001",
            _ => "DEV001"
        };
        AuthorizeAs(client, role, principalId);
    }

    private void AuthorizeAs(HttpClient client, string role, string principalId)
    {
        using var scope = _factory.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
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
    public Func<AuthClaims, int, int, CancellationToken, Task<PagedResponse<WalletHistoryEntry>>>? ListWalletHistory { get; init; }
    public Func<AuthClaims, string, CancellationToken, Task<WalletHistoryEntry>>? GetWalletHistoryEntry { get; init; }

    public Task<WalletSummary> GetWalletAsync(AuthClaims claims, CancellationToken cancellationToken) =>
        GetWallet?.Invoke(claims, cancellationToken) ?? throw new NotImplementedException();

    public Task<RechargeWalletResult> RechargeWalletAsync(AuthClaims claims, RechargeWalletRequest request, CancellationToken cancellationToken) =>
        RechargeWallet?.Invoke(claims, request, cancellationToken) ?? throw new NotImplementedException();

    public Task<PagedResponse<WalletTransactionEntry>> ListWalletTransactionsAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken) =>
        ListWalletTransactions?.Invoke(claims, page, pageSize, cancellationToken) ?? throw new NotImplementedException();

    public Task<PagedResponse<WalletHistoryEntry>> ListWalletHistoryAsync(AuthClaims claims, int page, int pageSize, CancellationToken cancellationToken) =>
        ListWalletHistory?.Invoke(claims, page, pageSize, cancellationToken) ?? throw new NotImplementedException();

    public Task<WalletHistoryEntry> GetWalletHistoryEntryAsync(AuthClaims claims, string historyId, CancellationToken cancellationToken) =>
        GetWalletHistoryEntry?.Invoke(claims, historyId, cancellationToken) ?? throw new NotImplementedException();

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
