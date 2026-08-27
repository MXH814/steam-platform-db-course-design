using System.Net;
using System.Text.Json;

namespace SteamPlatform.HttpsDeploy;

public sealed class HttpsDeploymentService(ProcessRunner processRunner)
{
    public const string ProductionCertificateName = "steam-platform-ip";
    public const string StagingCertificateName = "steam-platform-ip-staging";

    private const string ApplicationRoot = "/opt/steam-platform";
    private const string WebRoot = ApplicationRoot + "/www";
    private const string ToolRoot = ApplicationRoot + "/tools/certbot";
    private const string CertbotPackages = ToolRoot + "/packages";
    private const string Python = "/usr/bin/python3";
    private const string CertbotBootstrap = "from certbot.main import main; raise SystemExit(main())";
    private const string NginxAvailableConfig = "/etc/nginx/sites-available/steam-platform";
    private const string NginxEnabledConfig = "/etc/nginx/sites-enabled/steam-platform";
    private const string StateDirectory = "/var/lib/steam-platform-https";
    private const string StatePath = StateDirectory + "/state.json";
    private const string RenewalService = "/etc/systemd/system/steam-platform-certbot-renew.service";
    private const string RenewalTimer = "/etc/systemd/system/steam-platform-certbot-renew.timer";

    public void PrintPlan()
    {
        Console.WriteLine("Steam Platform trusted IP HTTPS deployment plan");
        Console.WriteLine("1. Require Linux root and explicit confirmation phrase.");
        Console.WriteLine("2. Install Certbot 5.7.0 into an isolated project package directory.");
        Console.WriteLine("3. Request a Let's Encrypt short-lived IP certificate through HTTP-01 webroot.");
        Console.WriteLine("4. Back up the active Nginx site and validate the generated TLS configuration.");
        Console.WriteLine("5. Redirect HTTP to HTTPS while preserving ACME challenge access.");
        Console.WriteLine("6. Renew hourly through a dedicated systemd timer and reload Nginx.");
        Console.WriteLine("7. Verify HTTP redirect, trusted TLS, frontend, API, Oracle health, and timer state.");
        Console.WriteLine("Rollback restores the exact pre-HTTPS Nginx site recorded in state.json.");
    }

    public async Task StageAsync(string publicIp, string acmeEmail, CancellationToken cancellationToken = default)
    {
        EnsureLinuxRoot();
        var timerState = await CaptureSystemTimerStateAsync(cancellationToken);
        await processRunner.IsSuccessfulAsync("systemctl", ["stop", "certbot.service"], cancellationToken);
        await processRunner.IsSuccessfulAsync("systemctl", ["disable", "--now", "certbot.timer"], cancellationToken);
        try
        {
            await EnsureCertbotAsync(cancellationToken);
            Directory.CreateDirectory(Path.Combine(WebRoot, ".well-known", "acme-challenge"));
            await VerifyAcmeWebRootAsync(publicIp, cancellationToken);
            await RequestCertificateAsync(publicIp, acmeEmail, StagingCertificateName, staging: true, cancellationToken);
        }
        finally
        {
            await RestoreSystemTimerAsync(timerState, cancellationToken);
        }

        Console.WriteLine("Let's Encrypt staging IP certificate issued successfully. Public Nginx traffic was not changed.");
    }

    public async Task EnableAsync(string publicIp, string acmeEmail, CancellationToken cancellationToken = default)
    {
        EnsureLinuxRoot();
        if (File.Exists(StatePath))
        {
            throw new InvalidOperationException($"HTTPS deployment state already exists at {StatePath}. Verify or roll back before enabling again.");
        }

        Directory.CreateDirectory(StateDirectory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        Directory.CreateDirectory(DeploymentBackupPaths.Directory);
        var backupPaths = DeploymentBackupPaths.Create(timestamp);
        File.Copy(NginxAvailableConfig, backupPaths.AvailableConfig, overwrite: false);
        File.Copy(NginxEnabledConfig, backupPaths.EnabledConfig, overwrite: false);
        var timerState = await CaptureSystemTimerStateAsync(cancellationToken);
        var state = new DeploymentState(
            publicIp,
            backupPaths.AvailableConfig,
            backupPaths.EnabledConfig,
            DateTimeOffset.UtcNow,
            timerState.Enabled,
            timerState.Active);
        await File.WriteAllTextAsync(StatePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

        try
        {
            await processRunner.IsSuccessfulAsync("systemctl", ["stop", "certbot.service"], cancellationToken);
            await processRunner.RunAsync("systemctl", ["disable", "--now", "certbot.timer"], cancellationToken);
            await EnsureCertbotAsync(cancellationToken);
            Directory.CreateDirectory(Path.Combine(WebRoot, ".well-known", "acme-challenge"));
            await VerifyAcmeWebRootAsync(publicIp, cancellationToken);
            await RequestCertificateAsync(publicIp, acmeEmail, ProductionCertificateName, staging: false, cancellationToken);

            var nginxConfig = NginxConfigRenderer.Render(publicIp);
            await File.WriteAllTextAsync(NginxAvailableConfig, nginxConfig, cancellationToken);
            await File.WriteAllTextAsync(NginxEnabledConfig, nginxConfig, cancellationToken);
            await WriteRenewalUnitsAsync(cancellationToken);
            await processRunner.RunAsync("nginx", ["-t"], cancellationToken);
            await processRunner.RunAsync("systemctl", ["daemon-reload"], cancellationToken);
            await processRunner.RunAsync("systemctl", ["enable", "--now", "steam-platform-certbot-renew.timer"], cancellationToken);
            await processRunner.RunAsync("systemctl", ["reload", "nginx"], cancellationToken);
            await VerifyAsync(publicIp, cancellationToken);
        }
        catch (Exception deploymentException)
        {
            try
            {
                await RestoreDeploymentAsync(state, cancellationToken);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "HTTPS enable failed and automatic rollback also failed. Keep state.json and use the documented manual recovery steps.",
                    deploymentException,
                    rollbackException);
            }

            throw new InvalidOperationException("HTTPS enable failed. The original Nginx and Certbot timer state was restored.", deploymentException);
        }

        Console.WriteLine($"Trusted HTTPS enabled at https://{publicIp}/.");
        Console.WriteLine($"Rollback backups: {backupPaths.AvailableConfig}; {backupPaths.EnabledConfig}");
    }

    public async Task VerifyAsync(string publicIp, CancellationToken cancellationToken = default)
    {
        await processRunner.RunAsync("nginx", ["-t"], cancellationToken);
        await processRunner.RunAsync("systemctl", ["is-active", "nginx"], cancellationToken);
        await processRunner.RunAsync("systemctl", ["is-active", "steam-platform-certbot-renew.timer"], cancellationToken);

        using var redirectClient = LoopbackHttpClientFactory.Create(allowAutoRedirect: false, TimeSpan.FromSeconds(15));
        using var redirectResponse = await redirectClient.GetAsync($"http://{publicIp}/api/health", cancellationToken);
        if (redirectResponse.StatusCode != HttpStatusCode.PermanentRedirect ||
            redirectResponse.Headers.Location?.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"HTTP redirect probe failed: status={(int)redirectResponse.StatusCode}, location={redirectResponse.Headers.Location}.");
        }
        Console.WriteLine($"PASS http://{publicIp}/api/health -> 308 HTTPS redirect");

        using var httpsClient = LoopbackHttpClientFactory.Create(allowAutoRedirect: false, TimeSpan.FromSeconds(30));
        foreach (var path in new[] { "/", "/api/health", "/health/database" })
        {
            try
            {
                using var response = await httpsClient.GetAsync($"https://{publicIp}{path}", cancellationToken);
                response.EnsureSuccessStatusCode();
                Console.WriteLine($"PASS https://{publicIp}{path} -> {(int)response.StatusCode}");
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Trusted loopback HTTPS probe failed for {path}: {exception.Message}", exception);
            }
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        EnsureLinuxRoot();
        if (!File.Exists(StatePath))
        {
            throw new InvalidOperationException($"No HTTPS deployment state exists at {StatePath}.");
        }

        var state = JsonSerializer.Deserialize<DeploymentState>(await File.ReadAllTextAsync(StatePath, cancellationToken))
            ?? throw new InvalidOperationException("HTTPS deployment state is invalid.");
        if (!File.Exists(state.AvailableConfigBackupPath) || !File.Exists(state.EnabledConfigBackupPath))
        {
            throw new FileNotFoundException("One or more recorded Nginx rollback backups are missing.");
        }

        await RestoreDeploymentAsync(state, cancellationToken);
        Console.WriteLine(
            $"Nginx restored from {state.AvailableConfigBackupPath} and {state.EnabledConfigBackupPath}. " +
            "Certificate files were retained for audit and safe reuse.");
    }

    private async Task EnsureCertbotAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(CertbotPackages);
        await processRunner.RunAsync(
            Python,
            ["-m", "pip", "install", "--disable-pip-version-check", "--upgrade", "--target", CertbotPackages, "certbot==5.7.0"],
            cancellationToken);
        await RunCertbotAsync(["--version"], cancellationToken);
    }

    private async Task RequestCertificateAsync(
        string publicIp,
        string acmeEmail,
        string certificateName,
        bool staging,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "certonly",
            "--webroot",
            "--webroot-path", WebRoot,
            "--ip-address", publicIp,
            "--preferred-profile", "shortlived",
            "--cert-name", certificateName,
            "--non-interactive",
            "--agree-tos",
            "--no-eff-email",
            "--keep-until-expiring",
            "--email", acmeEmail
        };
        if (staging)
        {
            arguments.Add("--staging");
            arguments.Add("--no-autorenew");
        }
        else
        {
            arguments.Add("--deploy-hook");
            arguments.Add("/usr/bin/systemctl reload nginx");
        }

        await RunCertbotAsync(arguments, cancellationToken);
    }

    private Task RunCertbotAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var commandArguments = new List<string> { "-c", CertbotBootstrap };
        commandArguments.AddRange(arguments);
        return processRunner.RunAsync(
            Python,
            commandArguments,
            cancellationToken,
            new Dictionary<string, string> { ["PYTHONPATH"] = CertbotPackages });
    }

    private static async Task WriteRenewalUnitsAsync(CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(RenewalService, SystemdUnitRenderer.RenderRenewalService(), cancellationToken);
        await File.WriteAllTextAsync(RenewalTimer, SystemdUnitRenderer.RenderRenewalTimer(), cancellationToken);
    }

    private static void EnsureLinuxRoot()
    {
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("HTTPS deployment commands run only on the Ubuntu server.");
        }

        if (!string.Equals(Environment.UserName, "root", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Run the deployment tool with sudo so Nginx and systemd changes are auditable.");
        }
    }

    private static async Task VerifyAcmeWebRootAsync(string publicIp, CancellationToken cancellationToken)
    {
        var challengeDirectory = Path.Combine(WebRoot, ".well-known", "acme-challenge");
        var token = $"steam-platform-{Guid.NewGuid():N}";
        var probePath = Path.Combine(challengeDirectory, token);
        await File.WriteAllTextAsync(probePath, token, cancellationToken);
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var response = await client.GetStringAsync($"http://{publicIp}/.well-known/acme-challenge/{token}", cancellationToken);
            if (!string.Equals(response.Trim(), token, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The public ACME webroot probe returned unexpected content.");
            }
        }
        finally
        {
            File.Delete(probePath);
        }
    }

    private async Task<SystemTimerState> CaptureSystemTimerStateAsync(CancellationToken cancellationToken) =>
        new(
            await processRunner.IsSuccessfulAsync("systemctl", ["is-enabled", "certbot.timer"], cancellationToken),
            await processRunner.IsSuccessfulAsync("systemctl", ["is-active", "certbot.timer"], cancellationToken));

    private async Task RestoreDeploymentAsync(DeploymentState state, CancellationToken cancellationToken)
    {
        File.Copy(state.AvailableConfigBackupPath, NginxAvailableConfig, overwrite: true);
        File.Copy(state.EnabledConfigBackupPath, NginxEnabledConfig, overwrite: true);
        await processRunner.IsSuccessfulAsync("systemctl", ["disable", "--now", "steam-platform-certbot-renew.timer"], cancellationToken);
        await RestoreSystemTimerAsync(new SystemTimerState(state.SystemCertbotTimerEnabled, state.SystemCertbotTimerActive), cancellationToken);
        await processRunner.RunAsync("nginx", ["-t"], cancellationToken);
        await processRunner.RunAsync("systemctl", ["reload", "nginx"], cancellationToken);
        File.Delete(StatePath);
    }

    private async Task RestoreSystemTimerAsync(SystemTimerState state, CancellationToken cancellationToken)
    {
        if (state.Enabled)
        {
            await processRunner.RunAsync("systemctl", ["enable", "certbot.timer"], cancellationToken);
        }
        if (state.Active)
        {
            await processRunner.RunAsync("systemctl", ["start", "certbot.timer"], cancellationToken);
        }
    }

    private sealed record SystemTimerState(bool Enabled, bool Active);
}
