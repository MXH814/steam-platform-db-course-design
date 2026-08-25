using System.Text.Json;
using System.Text.RegularExpressions;

namespace SteamPlatform.DemoData;

public sealed class DemoDataManifest
{
    private static readonly Regex IdentifierPattern = new("^[A-Z][A-Z0-9_]{0,127}$", RegexOptions.Compiled);

    public string BaselineScript { get; init; } = string.Empty;
    public IReadOnlyList<string> InsertionOrder { get; init; } = [];
    public IReadOnlyDictionary<string, int> MinimumRows { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<string> DeletionOrder => InsertionOrder.Reverse().ToArray();

    public static DemoDataManifest Load(string repositoryRoot, string manifestPath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(repositoryRoot, manifestPath));
        var manifest = JsonSerializer.Deserialize<DemoDataManifest>(
            File.ReadAllText(absolutePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Demo data manifest is empty.");

        manifest.Validate(repositoryRoot);
        return manifest;
    }

    public string ResolveBaselinePath(string repositoryRoot) =>
        Path.GetFullPath(Path.Combine(repositoryRoot, BaselineScript));

    public void Validate(string repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(BaselineScript))
        {
            throw new InvalidOperationException("baselineScript is required.");
        }

        if (!File.Exists(ResolveBaselinePath(repositoryRoot)))
        {
            throw new FileNotFoundException("Baseline SQL script was not found.", ResolveBaselinePath(repositoryRoot));
        }

        if (InsertionOrder.Count == 0 || InsertionOrder.Distinct(StringComparer.OrdinalIgnoreCase).Count() != InsertionOrder.Count)
        {
            throw new InvalidOperationException("insertionOrder must contain unique table names.");
        }

        foreach (var table in InsertionOrder)
        {
            if (!IdentifierPattern.IsMatch(table))
            {
                throw new InvalidOperationException($"Unsafe Oracle identifier in manifest: {table}");
            }
        }

        foreach (var (table, minimum) in MinimumRows)
        {
            if (!InsertionOrder.Contains(table, StringComparer.OrdinalIgnoreCase) || minimum < 0)
            {
                throw new InvalidOperationException($"Invalid minimumRows entry: {table}={minimum}");
            }
        }
    }
}
