using System.Globalization;
using System.Text.RegularExpressions;

namespace SteamPlatform.Database.Tests;

public sealed class MigrationScriptConventionTests
{
    [Theory]
    [MemberData(nameof(MigrationScriptNames))]
    public void Migration_scripts_use_current_date_and_slug_file_name(string scriptName)
    {
        Assert.Matches(@"^\d{8}_[a-z0-9_]+\.sql$", scriptName);

        var datePart = scriptName[..8];
        Assert.True(
            DateOnly.TryParseExact(datePart, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            $"Migration date prefix '{datePart}' is invalid.");
    }

    [Theory]
    [MemberData(nameof(MigrationScriptNames))]
    public void Migration_scripts_keep_commit_markers(string scriptName)
    {
        var sql = File.ReadAllText(Path.Combine(MigrationsDirectory.FullName, scriptName));

        Assert.Contains("COMMIT;", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(CurrentConventionMigrationScriptNames))]
    public void Current_migration_scripts_keep_sqlplus_setup_and_progress_markers(string scriptName)
    {
        var sql = File.ReadAllText(Path.Combine(MigrationsDirectory.FullName, scriptName));
        var firstNonEmptyLine = sql
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First();

        Assert.Equal("SET DEFINE OFF", firstNonEmptyLine);
        Assert.Contains("PROMPT ", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_script_names_are_unique_and_chronologically_ordered()
    {
        var names = GetMigrationScriptNames();

        Assert.NotEmpty(names);
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(names.Order(StringComparer.Ordinal).ToArray(), names);
    }

    [Fact]
    public void Migrations_do_not_create_or_drop_database_users()
    {
        foreach (var file in MigrationsDirectory.EnumerateFiles("*.sql"))
        {
            var normalized = Regex.Replace(File.ReadAllText(file.FullName), @"\s+", " ");

            Assert.DoesNotContain("CREATE USER ", normalized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP USER ", normalized, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static TheoryData<string> MigrationScriptNames()
    {
        var data = new TheoryData<string>();
        foreach (var scriptName in GetMigrationScriptNames())
        {
            data.Add(scriptName);
        }

        return data;
    }

    public static TheoryData<string> CurrentConventionMigrationScriptNames()
    {
        var data = new TheoryData<string>();
        foreach (var scriptName in GetMigrationScriptNames().Where(name => string.CompareOrdinal(name, "20260825_") >= 0))
        {
            data.Add(scriptName);
        }

        return data;
    }

    private static IReadOnlyList<string> GetMigrationScriptNames() =>
        MigrationsDirectory
            .EnumerateFiles("*.sql")
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .Select(file => file.Name)
            .ToArray();

    private static DirectoryInfo MigrationsDirectory =>
        new(Path.Combine(SqlFile.RepositoryRoot, "database", "migrations"));
}
