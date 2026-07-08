using SteamPlatform.Shared;

namespace SteamPlatform.Application.Games;

public sealed class GameService(IGameRepository repository) : IGameService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ONLINE",
        "OFFLINE"
    };

    private static readonly HashSet<string> AllowedReputations = new(StringComparer.OrdinalIgnoreCase)
    {
        "OVERWHELMINGLY_POSITIVE",
        "VERY_POSITIVE",
        "MOSTLY_POSITIVE",
        "MIXED",
        "NEGATIVE"
    };

    private readonly IGameRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task<PagedResponse<GameListItemResponse>> ListAsync(GameListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = NormalizeOptional(query.Status)?.ToUpperInvariant() ?? "ONLINE";
        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException("Game status must be ONLINE or OFFLINE.");
        }

        var reputation = NormalizeOptional(query.Reputation)?.ToUpperInvariant();
        if (reputation is not null && !AllowedReputations.Contains(reputation))
        {
            throw new ArgumentException("Game reputation is not supported.");
        }

        if (query.MinPrice is < 0 || query.MaxPrice is < 0)
        {
            throw new ArgumentException("Price filters must be greater than or equal to 0.");
        }

        if (query.MinPrice is not null && query.MaxPrice is not null && query.MinPrice > query.MaxPrice)
        {
            throw new ArgumentException("MinPrice must be less than or equal to MaxPrice.");
        }

        var normalized = query with
        {
            Keyword = NormalizeOptional(query.Keyword),
            Status = status,
            DeveloperId = NormalizeOptional(query.DeveloperId),
            Reputation = reputation,
            Page = page,
            PageSize = pageSize
        };

        return _repository.ListAsync(normalized, cancellationToken);
    }

    public async Task<GameDetailResponse> GetDetailAsync(string gameId, CancellationToken cancellationToken) =>
        await _repository.GetDetailAsync(NormalizeRequired(gameId, nameof(gameId)), cancellationToken)
            ?? throw new ResourceNotFoundException("Game does not exist.");

    public async Task<ReviewSummaryResponse> GetReviewSummaryAsync(string gameId, CancellationToken cancellationToken)
    {
        await EnsureGameExistsAsync(gameId, cancellationToken);
        return await _repository.GetReviewSummaryAsync(NormalizeRequired(gameId, nameof(gameId)), cancellationToken);
    }

    public async Task<AchievementSummaryResponse> GetAchievementSummaryAsync(string gameId, CancellationToken cancellationToken)
    {
        await EnsureGameExistsAsync(gameId, cancellationToken);
        return await _repository.GetAchievementSummaryAsync(NormalizeRequired(gameId, nameof(gameId)), cancellationToken);
    }

    public async Task<GameDetailResponse> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = await NormalizeCreateRequestAsync(request, cancellationToken);
        var gameId = IdGenerator.NewId("G");
        return await _repository.CreateAsync(gameId, normalized, cancellationToken);
    }

    public async Task<GameDetailResponse> UpdateAsync(string gameId, string developerId, UpdateGameRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        var normalizedDeveloperId = NormalizeRequired(developerId, nameof(developerId));
        var normalized = NormalizeUpdateRequest(request);
        var existing = await GetDetailAsync(normalizedGameId, cancellationToken);

        if (!string.Equals(existing.DeveloperId, normalizedDeveloperId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("GAME_DEVELOPER_MISMATCH", "Developer can only update games owned by itself.");
        }

        var affected = await _repository.UpdateAsync(normalizedGameId, normalizedDeveloperId, normalized, cancellationToken);
        if (!affected)
        {
            throw new ResourceNotFoundException("Game does not exist.");
        }

        return await GetDetailAsync(normalizedGameId, cancellationToken);
    }

    public async Task<GameDetailResponse> SetStatusAsync(string gameId, string status, CancellationToken cancellationToken)
    {
        var normalizedGameId = NormalizeRequired(gameId, nameof(gameId));
        var normalizedStatus = NormalizeStatus(status);

        var affected = await _repository.SetStatusAsync(normalizedGameId, normalizedStatus, cancellationToken);
        if (!affected)
        {
            throw new ResourceNotFoundException("Game does not exist.");
        }

        return await GetDetailAsync(normalizedGameId, cancellationToken);
    }

    private async Task<CreateGameRequest> NormalizeCreateRequestAsync(CreateGameRequest request, CancellationToken cancellationToken)
    {
        var developerId = NormalizeRequired(request.DevId, nameof(request.DevId));
        if (!await _repository.DeveloperExistsAsync(developerId, cancellationToken))
        {
            throw new ArgumentException("Developer does not exist.");
        }

        return request with
        {
            DevId = developerId,
            GameName = NormalizeRequired(request.GameName, nameof(request.GameName)),
            Reputation = NormalizeReputation(request.Reputation),
            BasePrice = NormalizeBasePrice(request.BasePrice),
            DiscountRate = NormalizeDiscountRate(request.DiscountRate),
            ReleaseDate = request.ReleaseDate.Date
        };
    }

    private static UpdateGameRequest NormalizeUpdateRequest(UpdateGameRequest request) =>
        request with
        {
            GameName = NormalizeRequired(request.GameName, nameof(request.GameName)),
            Reputation = NormalizeReputation(request.Reputation),
            BasePrice = NormalizeBasePrice(request.BasePrice),
            DiscountRate = NormalizeDiscountRate(request.DiscountRate),
            ReleaseDate = request.ReleaseDate.Date
        };

    private async Task EnsureGameExistsAsync(string gameId, CancellationToken cancellationToken)
    {
        _ = await GetDetailAsync(gameId, cancellationToken);
    }

    private static string NormalizeStatus(string value)
    {
        var status = NormalizeRequired(value, nameof(value)).ToUpperInvariant();
        return AllowedStatuses.Contains(status)
            ? status
            : throw new ArgumentException("Game status must be ONLINE or OFFLINE.");
    }

    private static string? NormalizeReputation(string? value)
    {
        var reputation = NormalizeOptional(value)?.ToUpperInvariant();
        return reputation is null || AllowedReputations.Contains(reputation)
            ? reputation
            : throw new ArgumentException("Game reputation is not supported.");
    }

    private static decimal NormalizeBasePrice(decimal value) =>
        value >= 0 ? value : throw new ArgumentException("BasePrice must be greater than or equal to 0.");

    private static decimal NormalizeDiscountRate(decimal value) =>
        value is >= 0 and <= 1 ? value : throw new ArgumentException("DiscountRate must be between 0 and 1.");

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
