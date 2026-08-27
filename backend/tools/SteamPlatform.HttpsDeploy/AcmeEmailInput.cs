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
        if (!string.IsNullOrWhiteSpace(options.AcmeEmailFile))
        {
            if (!Path.IsPathRooted(options.AcmeEmailFile))
            {
                throw new ArgumentException("--email-file must use an absolute path.");
            }

            var path = Path.GetFullPath(options.AcmeEmailFile);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("--email-file does not exist.", path);
            }

            if (OperatingSystem.IsLinux())
            {
                var mode = File.GetUnixFileMode(path);
                const UnixFileMode disallowed = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                                                UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
                if ((mode & disallowed) != 0)
                {
                    throw new UnauthorizedAccessException("--email-file must not grant group or other permissions.");
                }
            }

            email = await File.ReadAllTextAsync(path, cancellationToken);
        }
        else if (options.ReadEmailFromStandardInput)
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
