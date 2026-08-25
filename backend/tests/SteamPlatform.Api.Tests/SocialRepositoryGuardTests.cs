namespace SteamPlatform.Api.Tests;

public sealed class SocialRepositoryGuardTests
{
    [Fact]
    public void Workshop_query_does_not_group_by_clob_details()
    {
        var source = File.ReadAllText(FindRepositorySource());

        Assert.Contains("subscription_totals", source, StringComparison.Ordinal);
        Assert.DoesNotContain("group by wi.workshop_item_id", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositorySource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "SteamPlatform.Infrastructure", "Social", "SocialRepository.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(directory.FullName, "backend", "src", "SteamPlatform.Infrastructure", "Social", "SocialRepository.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("SocialRepository.cs was not found.");
    }
}
