using System.Security.Cryptography;
using System.Text;
using SteamPlatform.DemoData;

try
{
    var options = CliOptions.Parse(args);
    var repositoryRoot = RepositoryRoot.Find(options.Root);
    var manifest = DemoDataManifest.Load(repositoryRoot, options.ManifestPath);
    var baselinePath = manifest.ResolveBaselinePath(repositoryRoot);
    var baseline = File.ReadAllText(baselinePath);
    var baselineStatements = SqlScriptParser.ParseBaseline(baseline);
    var baselineHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(baseline)));

    if (options.Command == "plan")
    {
        Console.WriteLine("Steam Platform demo-data recovery plan");
        Console.WriteLine($"Baseline: {Path.GetRelativePath(repositoryRoot, baselinePath)}");
        Console.WriteLine($"SHA-256: {baselineHash}");
        Console.WriteLine($"Business tables: {manifest.InsertionOrder.Count}");
        Console.WriteLine($"Baseline INSERT statements: {baselineStatements.Count}");
        Console.WriteLine("Reset safety: snapshot -> transaction reset -> minimum-count validation -> commit");
        Console.WriteLine("Restore safety: transaction delete -> snapshot restore -> exact-count validation -> commit");
        return 0;
    }

    var connectionString = Environment.GetEnvironmentVariable(options.ConnectionEnvironmentVariable);
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException($"Environment variable {options.ConnectionEnvironmentVariable} is not set. The tool never accepts a connection string on the command line.");
    }

    var service = new DemoDataResetService(connectionString, manifest, repositoryRoot);
    switch (options.Command)
    {
        case "reset":
            {
                var runId = await service.ResetAsync(options.Actor, options.Confirmation);
                Console.WriteLine($"Demo baseline reset completed. Run id: {runId}");
                Console.WriteLine($"Rollback command uses --run-id {runId} --confirm {DemoDataResetService.RequiredRestoreConfirmation}");
                break;
            }
        case "restore":
            if (string.IsNullOrWhiteSpace(options.RunId))
            {
                throw new ArgumentException("restore requires --run-id.");
            }

            await service.RestoreAsync(options.RunId, options.Actor, options.Confirmation);
            Console.WriteLine($"Snapshot {options.RunId} restored and verified.");
            break;
        case "list":
            foreach (var run in await service.ListRunsAsync())
            {
                Console.WriteLine($"{run.RunId}  {run.Status,-18}  {run.StartedAt:O}  {run.InitiatedBy}");
            }

            break;
        default:
            throw new ArgumentException("Unknown command. Use plan, reset, restore, or list.");
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    return 1;
}
