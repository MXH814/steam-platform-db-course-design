namespace SteamPlatform.Database.Tests;

internal static class SqlFile
{
    public static string RepositoryRoot => FindRepositoryRoot();
    public static string Schema => Read("database", "schema.sql");
    public static string Data => Read("database", "data.sql");
    public static string VerifyPhase1 => Read("database", "verify_phase1.sql");
    public static string VerifyDefense => Read("database", "verify_defense.sql");
    public static string ExplainPlans => Read("database", "defense", "explain_plans.sql");
    public static string LockSessionA => Read("database", "defense", "lock_session_a.sql");
    public static string LockSessionB => Read("database", "defense", "lock_session_b.sql");
    public static string WalletPaymentMethodMigration => Read("database", "migrations", "20260712_wallet_payment_method_history.sql");
    public static string GroupDAchievementMigration => Read("database", "migrations", "20260713_group_d_achievement_seed.sql");
    public static string DemoResetAuditMigration => Read("database", "migrations", "20260825_demo_reset_audit.sql");
    public static string SocialRealtimeMigration => Read("database", "migrations", "20260825_social_realtime_foundation.sql");
    public static string CommunityEngagementMigration => Read("database", "migrations", "20260825_community_engagement_expansion.sql");

    private static string Read(params string[] path)
    {
        return File.ReadAllText(Path.Combine([RepositoryRoot, .. path]));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "database")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
