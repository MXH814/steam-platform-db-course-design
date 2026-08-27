using System.Net;
using System.Net.Sockets;

namespace SteamPlatform.HttpsDeploy;

public sealed record HttpsDeployOptions(
    string Command,
    string? PublicIp,
    string? AcmeEmail,
    string? Confirmation)
{
    public const string StageConfirmation = "STAGE_IP_HTTPS";
    public const string EnableConfirmation = "ENABLE_IP_HTTPS";
    public const string RollbackConfirmation = "ROLLBACK_IP_HTTPS";

    public static HttpsDeployOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Command is required: plan, render, stage, enable, verify, or rollback.");
        }

        var command = args[0].Trim().ToLowerInvariant();
        if (command is not ("plan" or "render" or "stage" or "enable" or "verify" or "rollback"))
        {
            throw new ArgumentException("Unknown command. Use plan, render, stage, enable, verify, or rollback.");
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Invalid option near '{args[index]}'. Options require --name value pairs.");
            }

            values[args[index][2..]] = args[index + 1];
        }

        var publicIp = Value(values, "ip") ?? Environment.GetEnvironmentVariable("STEAM_HTTPS_PUBLIC_IP");
        var acmeEmail = Value(values, "email") ?? Environment.GetEnvironmentVariable("STEAM_HTTPS_ACME_EMAIL");
        var confirmation = Value(values, "confirm");

        if (command is "render" or "stage" or "enable" or "verify")
        {
            ValidatePublicIp(publicIp);
        }

        if ((command is "stage" or "enable") && string.IsNullOrWhiteSpace(acmeEmail))
        {
            throw new ArgumentException("ACME email is required through --email or STEAM_HTTPS_ACME_EMAIL.");
        }

        var requiredConfirmation = command switch
        {
            "stage" => StageConfirmation,
            "enable" => EnableConfirmation,
            "rollback" => RollbackConfirmation,
            _ => null
        };
        if (requiredConfirmation is not null && !string.Equals(confirmation, requiredConfirmation, StringComparison.Ordinal))
        {
            throw new ArgumentException($"{command} requires --confirm {requiredConfirmation}.");
        }

        return new HttpsDeployOptions(command, publicIp, acmeEmail, confirmation);
    }

    private static string? Value(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value.Trim() : null;

    private static void ValidatePublicIp(string? value)
    {
        if (!IPAddress.TryParse(value, out var address) ||
            address.AddressFamily != AddressFamily.InterNetwork ||
            IPAddress.IsLoopback(address) ||
            IsPrivate(address))
        {
            throw new ArgumentException("--ip must be a public IPv4 address.");
        }
    }

    private static bool IsPrivate(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
               bytes[0] == 127 ||
               bytes[0] == 0 ||
               bytes[0] >= 224 ||
               bytes[0] == 169 && bytes[1] == 254 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
               bytes[0] == 192 && bytes[1] == 168;
    }
}
