namespace SteamPlatform.DemoData;

public static class RepositoryRoot
{
    public static string Find(string? explicitRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            return Validate(Path.GetFullPath(explicitRoot));
        }

        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            if (IsRepositoryRoot(directory.FullName))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root was not found. Run the tool inside the repository or pass --root.");
    }

    private static string Validate(string path) =>
        IsRepositoryRoot(path)
            ? path
            : throw new InvalidOperationException($"Not a Steam Platform repository root: {path}");

    private static bool IsRepositoryRoot(string path) =>
        File.Exists(Path.Combine(path, "README.md")) &&
        File.Exists(Path.Combine(path, "database", "schema.sql")) &&
        Directory.Exists(Path.Combine(path, "backend"));
}
