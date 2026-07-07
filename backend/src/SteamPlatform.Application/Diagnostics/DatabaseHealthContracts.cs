namespace SteamPlatform.Application.Diagnostics;

public sealed record DatabaseHealthResult(string Status, string? Database = null, string? Reason = null);

public interface IDatabaseHealthProbe
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken);
}
