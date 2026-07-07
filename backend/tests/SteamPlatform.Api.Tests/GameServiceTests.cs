using SteamPlatform.Application.Games;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class GameServiceTests
{
    [Fact]
    public async Task List_defaults_to_online_games()
    {
        var repository = new RecordingGameRepository();
        var service = new GameService(repository);

        await service.ListAsync(new GameListQuery(null, null, null, null, null, null), CancellationToken.None);

        Assert.Equal("ONLINE", repository.LastListQuery?.Status);
        Assert.Equal(1, repository.LastListQuery?.Page);
        Assert.Equal(20, repository.LastListQuery?.PageSize);
    }

    [Fact]
    public async Task List_rejects_unknown_reputation()
    {
        var service = new GameService(new RecordingGameRepository());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ListAsync(new GameListQuery(null, null, null, null, null, "GREAT"), CancellationToken.None));

        Assert.Contains("reputation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_requires_existing_developer()
    {
        var service = new GameService(new RecordingGameRepository { DeveloperExists = false });

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateGameRequest("DEV404", "New Game", 10m, 1m, DateTime.Today, null, "OFFLINE"), CancellationToken.None));

        Assert.Contains("Developer does not exist.", exception.Message);
    }

    [Fact]
    public async Task Update_rejects_invalid_discount_rate_before_repository_write()
    {
        var repository = new RecordingGameRepository();
        var service = new GameService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateAsync("G001", new UpdateGameRequest("Game", 10m, 1.5m, DateTime.Today, null, "ONLINE"), CancellationToken.None));

        Assert.False(repository.UpdateCalled);
    }

    private sealed class RecordingGameRepository : IGameRepository
    {
        public bool DeveloperExists { get; init; } = true;
        public bool UpdateCalled { get; private set; }
        public GameListQuery? LastListQuery { get; private set; }

        public Task<PagedResponse<GameListItemResponse>> ListAsync(GameListQuery query, CancellationToken cancellationToken)
        {
            LastListQuery = query;
            return Task.FromResult(new PagedResponse<GameListItemResponse>([], query.Page, query.PageSize, 0));
        }

        public Task<GameDetailResponse?> GetDetailAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult<GameDetailResponse?>(NewGame(gameId));

        public Task<ReviewSummaryResponse> GetReviewSummaryAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult(new ReviewSummaryResponse(0, 0, 0, null));

        public Task<AchievementSummaryResponse> GetAchievementSummaryAsync(string gameId, CancellationToken cancellationToken) =>
            Task.FromResult(new AchievementSummaryResponse(0, null, []));

        public Task<bool> DeveloperExistsAsync(string developerId, CancellationToken cancellationToken) =>
            Task.FromResult(DeveloperExists);

        public Task<GameDetailResponse> CreateAsync(string gameId, CreateGameRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(NewGame(gameId));

        public Task<bool> UpdateAsync(string gameId, UpdateGameRequest request, CancellationToken cancellationToken)
        {
            UpdateCalled = true;
            return Task.FromResult(true);
        }

        public Task<bool> SetStatusAsync(string gameId, string status, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        private static GameDetailResponse NewGame(string gameId) =>
            new(gameId, "Game", "DEV001", "Developer", 10m, 1m, 10m, DateTime.Today, null, "ONLINE");
    }
}
