namespace SteamPlatform.DemoData;

public sealed class CliOptions
{
    public string Command { get; private init; } = "plan";
    public string ManifestPath { get; private init; } = Path.Combine("database", "demo", "manifest.json");
    public string ConnectionEnvironmentVariable { get; private init; } = "STEAM_ORACLE_ADMIN_CONNECTION";
    public string? Root { get; private init; }
    public string? Confirmation { get; private init; }
    public string? RunId { get; private init; }
    public string Actor { get; private init; } = Environment.UserName;

    public static CliOptions Parse(string[] args)
    {
        var command = args.FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal)) ?? "plan";
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Option {args[index]} requires a value.");
            }

            values[args[index]] = args[++index];
        }

        return new CliOptions
        {
            Command = command.ToLowerInvariant(),
            ManifestPath = Value(values, "--manifest") ?? Path.Combine("database", "demo", "manifest.json"),
            ConnectionEnvironmentVariable = Value(values, "--connection-env") ?? "STEAM_ORACLE_ADMIN_CONNECTION",
            Root = Value(values, "--root"),
            Confirmation = Value(values, "--confirm"),
            RunId = Value(values, "--run-id"),
            Actor = Value(values, "--actor") ?? Environment.UserName
        };
    }

    private static string? Value(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;
}
