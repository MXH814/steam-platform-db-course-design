namespace SteamPlatform.Database.Tests;

internal static class SqlFile
{
    public static string Schema => Read("database", "schema.sql");
    public static string Data => Read("database", "data.sql");
    public static string VerifyPhase1 => Read("database", "verify_phase1.sql");

    private static string Read(params string[] path)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine([root, .. path]));
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
