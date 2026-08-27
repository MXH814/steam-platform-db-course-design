using System.Net.Mail;

namespace SteamPlatform.HttpsDeploy;

public static class AcmeEmailInput
{
    public static async Task<string> ResolveAsync(
        HttpsDeployOptions options,
        TextReader? standardInput = null,
        CancellationToken cancellationToken = default)
    {
        var email = options.AcmeEmail;
        if (options.ReadEmailFromStandardInput)
        {
            standardInput ??= Console.In;
            email = await standardInput.ReadLineAsync(cancellationToken);
        }

        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out var parsed) ||
            !string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("ACME email is invalid.");
        }

        return parsed.Address;
    }
}
