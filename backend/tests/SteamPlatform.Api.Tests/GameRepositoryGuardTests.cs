namespace SteamPlatform.Api.Tests;

public sealed class GameRepositoryGuardTests
{
    [Fact]
    public void Achievement_average_is_rounded_before_decimal_mapping()
    {
        var source = File.ReadAllText(FindRepositorySource());

        Assert.Contains(
            "cast(round(avg(global_rate), 2) as number(7, 2)) as AverageGlobalRate",
            source,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositorySource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var direct = Path.Combine(directory.FullName, "src", "SteamPlatform.Infrastructure", "Games", "GameRepository.cs");
            if (File.Exists(direct)) return direct;

            var nested = Path.Combine(directory.FullName, "backend", "src", "SteamPlatform.Infrastructure", "Games", "GameRepository.cs");
            if (File.Exists(nested)) return nested;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("GameRepository.cs was not found.");
    }
}
