using System.Text;

namespace SteamPlatform.DemoData;

public static class SqlScriptParser
{
    private static readonly string[] SqlPlusPrefixes = ["SET ", "PROMPT ", "WHENEVER ", "SPOOL "];

    public static IReadOnlyList<string> ParseBaseline(string script)
    {
        var filtered = string.Join(
            '\n',
            script.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Where(line => !SqlPlusPrefixes.Any(prefix => line.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))));

        var statements = SplitStatements(filtered)
            .Where(statement => !statement.Equals("COMMIT", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var statement in statements)
        {
            if (!statement.StartsWith("INSERT INTO ", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Baseline contains a non-INSERT statement: {Preview(statement)}");
            }
        }

        if (statements.Length == 0)
        {
            throw new InvalidOperationException("Baseline does not contain any INSERT statements.");
        }

        return statements;
    }

    private static IReadOnlyList<string> SplitStatements(string script)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inString = false;

        for (var index = 0; index < script.Length; index++)
        {
            var character = script[index];
            if (character == '\'')
            {
                current.Append(character);
                if (inString && index + 1 < script.Length && script[index + 1] == '\'')
                {
                    current.Append(script[++index]);
                    continue;
                }

                inString = !inString;
                continue;
            }

            if (character == ';' && !inString)
            {
                AddStatement(result, current);
                continue;
            }

            current.Append(character);
        }

        if (inString)
        {
            throw new InvalidOperationException("Baseline contains an unterminated string literal.");
        }

        AddStatement(result, current);
        return result;
    }

    private static void AddStatement(ICollection<string> statements, StringBuilder current)
    {
        var statement = current.ToString().Trim();
        current.Clear();
        if (statement.Length > 0)
        {
            statements.Add(statement);
        }
    }

    private static string Preview(string statement) =>
        statement.Length <= 80 ? statement : string.Concat(statement.AsSpan(0, 77), "...");
}
