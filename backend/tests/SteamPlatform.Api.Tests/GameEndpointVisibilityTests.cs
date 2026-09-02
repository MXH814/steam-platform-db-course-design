using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Games;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class GameEndpointVisibilityTests(SteamPlatformApiFactory factory) : IClassFixture<SteamPlatformApiFactory>
{
    private readonly SteamPlatformApiFactory _factory = factory;

    [Fact]
    public async Task Anonymous_users_cannot_list_offline_games()
    {
        var service = new VisibilityGameService();
        using var client = CreateClient(service);

        using var response = await client.GetAsync("/api/games?status=OFFLINE");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(service.LastListQuery);
    }

    [Fact]
    public async Task Developer_offline_list_is_forced_to_its_own_identity()
    {
        var service = new VisibilityGameService();
        using var client = CreateClient(service, "DEVELOPER", "DEV_OWNER");

        using var response = await client.GetAsync("/api/games?status=OFFLINE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("DEV_OWNER", service.LastListQuery?.DeveloperId);
    }

    [Fact]
    public async Task Developer_cannot_list_another_developers_offline_games()
    {
        var service = new VisibilityGameService();
        using var client = CreateClient(service, "DEVELOPER", "DEV_OTHER");

        using var response = await client.GetAsync("/api/games?status=OFFLINE&developerId=DEV_OWNER");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(service.LastListQuery);
    }

    [Fact]
    public async Task Offline_detail_is_hidden_from_anonymous_users()
    {
        var service = new VisibilityGameService { GameStatus = "OFFLINE" };
        using var client = CreateClient(service);

        using var response = await client.GetAsync("/api/games/G_OFFLINE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Offline_detail_is_visible_to_owning_developer_and_admin()
    {
        var service = new VisibilityGameService { GameStatus = "OFFLINE" };
        using var developerClient = CreateClient(service, "DEVELOPER", "DEV_OWNER");
        using var adminClient = CreateClient(service, "SUPER_ADMIN", "ADM001");

        using var developerResponse = await developerClient.GetAsync("/api/games/G_OFFLINE");
        using var adminResponse = await adminClient.GetAsync("/api/games/G_OFFLINE");

        Assert.Equal(HttpStatusCode.OK, developerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Offline_summaries_are_hidden_from_public_users()
    {
        var service = new VisibilityGameService { GameStatus = "OFFLINE" };
        using var client = CreateClient(service);

        using var response = await client.GetAsync("/api/games/G_OFFLINE/reviews/summary");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(service.ReviewSummaryCalled);
    }

    [Fact]
    public async Task Online_detail_remains_public()
    {
        var service = new VisibilityGameService { GameStatus = "ONLINE" };
        using var client = CreateClient(service);

        using var response = await client.GetAsync("/api/games/G_ONLINE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClient(VisibilityGameService service, string? role = null, string principalId = "P001")
    {
        var application = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGameService>();
                services.AddSingleton<IGameService>(service);
            }));
        var client = application.CreateClient();
        if (role is not null)
        {
            using var scope = _factory.Services.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var token = auth.CreateToken(new AuthClaims(role, principalId, "visibility-test", DateTimeOffset.UtcNow.AddMinutes(10)));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private sealed class VisibilityGameService : IGameService
    {
        public string GameStatus { get; init; } = "ONLINE";
        public GameListQuery? LastListQuery { get; private set; }
        public bool ReviewSummaryCalled { get; private set; }

        public Task<PagedResponse<GameListItemResponse>> ListAsync(GameListQuery query, CancellationToken cancellationToken)
        {
            LastListQuery = query;
            return Task.FromResult(new PagedResponse<GameListItemResponse>([], query.Page, query.PageSize, 0));
        }

        public Task<GameDetailResponse> GetDetailAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult(new GameDetailResponse(gameId, "Game", "DEV_OWNER", "Owner", 10, 1, 10, DateTime.Today, null, GameStatus));

        public Task<ReviewSummaryResponse> GetReviewSummaryAsync(string gameId, CancellationToken cancellationToken)
        {
            ReviewSummaryCalled = true;
            return Task.FromResult(new ReviewSummaryResponse(0, 0, 0, null));
        }

        public Task<AchievementSummaryResponse> GetAchievementSummaryAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult(new AchievementSummaryResponse(0, null, []));

        public Task<IReadOnlyList<GameContentPackageResponse>> GetContentPackagesAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GameContentPackageResponse>>([]);

        public Task<GameItemSummaryResponse> GetItemSummaryAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult(new GameItemSummaryResponse(gameId, 0, 0, 0, 0, 0, null, null, null, []));

        public Task<GameDetailResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<GameDetailResponse> UpdateAsync(string gameId, string developerId, UpdateGameRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteAsync(string gameId, string developerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<GameDetailResponse> SetStatusAsync(string gameId, string status, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
