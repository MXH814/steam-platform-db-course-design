using SteamPlatform.DemoData;

namespace SteamPlatform.DemoData.Tests;

public sealed class DemoDataPlanTests
{
    private readonly string _root = RepositoryRoot.Find();

    [Fact]
    public void Manifest_covers_core_and_enhancement_business_tables_in_dependency_order()
    {
        var manifest = DemoDataManifest.Load(_root, Path.Combine("database", "demo", "manifest.json"));

        Assert.Equal(42, manifest.InsertionOrder.Count);
        Assert.Equal("PLAYER", manifest.InsertionOrder[0]);
        Assert.Equal("DISCUSSION_REPLY", manifest.InsertionOrder[^1]);
        Assert.Equal(manifest.InsertionOrder.Reverse(), manifest.DeletionOrder);
        Assert.DoesNotContain("DEMO_RESET_RUN", manifest.InsertionOrder);
    }

    [Fact]
    public void Baseline_parser_accepts_seed_file_and_removes_sqlplus_commands()
    {
        var manifest = DemoDataManifest.Load(_root, Path.Combine("database", "demo", "manifest.json"));
        var statements = SqlScriptParser.ParseBaseline(File.ReadAllText(manifest.ResolveBaselinePath(_root)));

        Assert.True(statements.Count >= 130, $"Expected a comprehensive baseline, got {statements.Count} statements.");
        Assert.All(statements, statement => Assert.StartsWith("INSERT INTO ", statement, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(statements, statement => statement.Contains("Don''t Starve Together", StringComparison.Ordinal));
        Assert.Contains(statements, statement => statement.Contains("ITPL_CS2_AWP_DRAGON_LORE", StringComparison.Ordinal));
    }

    [Fact]
    public void Baseline_parser_rejects_destructive_statements()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SqlScriptParser.ParseBaseline("DELETE FROM PLAYER; INSERT INTO PLAYER (user_id) VALUES ('P1');"));

        Assert.Contains("non-INSERT", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_parser_handles_escaped_quotes_and_semicolons_inside_text()
    {
        var statements = SqlScriptParser.ParseBaseline(
            "INSERT INTO T (value) VALUES ('Don''t stop; keep going'); COMMIT;");

        Assert.Single(statements);
        Assert.Contains("stop; keep", statements[0], StringComparison.Ordinal);
    }
}
