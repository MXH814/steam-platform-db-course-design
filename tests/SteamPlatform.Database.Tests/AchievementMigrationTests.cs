using System.Text.RegularExpressions;

namespace SteamPlatform.Database.Tests;

public sealed class AchievementMigrationTests
{
    private static readonly string[] ExpectedAchievementIds =
    [
        "ACH_DST_SURVIVE_001",
        "ACH_DST_SCIENCE_MACHINE",
        "ACH_DST_WINTER_SURVIVOR",
        "ACH_DST_RUINS_DIVER",
        "ACH_DST_SHADOW_DUEL",
        "ACH_DST_CELESTIAL_CARTOGRAPHER",
        "ACH_CS2_FIRST_ROUND",
        "ACH_CS2_ACE",
        "ACH_CS2_BOMB_PLANT",
        "ACH_CS2_DEFUSE",
        "ACH_CS2_MARKET_MAKER"
    ];

    [Fact]
    public void GroupD_achievement_migration_seeds_metadata_into_achievement_table()
    {
        var sql = SqlFile.GroupDAchievementMigration;

        Assert.Contains("MERGE INTO ACHIEVEMENT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHEN MATCHED THEN UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHEN NOT MATCHED THEN INSERT", sql, StringComparison.OrdinalIgnoreCase);

        foreach (var achievementId in ExpectedAchievementIds)
        {
            Assert.Contains($"'{achievementId}'", sql, StringComparison.OrdinalIgnoreCase);
        }

        var seededIds = Regex.Matches(sql, @"'ACH_(?:DST|CS2)_[A-Z0-9_]+'")
            .Select(match => match.Value.Trim('\''))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(ExpectedAchievementIds.Length, seededIds.Length);
        Assert.Empty(ExpectedAchievementIds.Except(seededIds, StringComparer.OrdinalIgnoreCase));
    }
}