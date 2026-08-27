using SteamPlatform.HttpsDeploy;
using System.Text.Json;

namespace SteamPlatform.HttpsDeploy.Tests;

public sealed class HttpsDeployTests
{
    [Fact]
    public void Parse_RejectsPrivateAddress()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            HttpsDeployOptions.Parse(["verify", "--ip", "192.168.1.20"]));

        Assert.Contains("public IPv4", exception.Message);
    }

    [Fact]
    public void Parse_RequiresExactEnableConfirmation()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            HttpsDeployOptions.Parse([
                "enable",
                "--ip", "124.222.213.245",
                "--email", "operator@example.com",
                "--confirm", "yes"
            ]));

        Assert.Contains(HttpsDeployOptions.EnableConfirmation, exception.Message);
    }

    [Fact]
    public void Parse_AcceptsVerifiedPublicIpPlan()
    {
        var options = HttpsDeployOptions.Parse([
            "stage",
            "--ip", "124.222.213.245",
            "--email", "operator@example.com",
            "--confirm", HttpsDeployOptions.StageConfirmation
        ]);

        Assert.Equal("stage", options.Command);
        Assert.Equal("124.222.213.245", options.PublicIp);
    }

    [Fact]
    public async Task EmailInput_ReadsValidAddressFromStandardInputWithoutCommandArgument()
    {
        var options = HttpsDeployOptions.Parse([
            "stage",
            "--ip", "124.222.213.245",
            "--email-stdin", "true",
            "--confirm", HttpsDeployOptions.StageConfirmation
        ]);

        var email = await AcmeEmailInput.ResolveAsync(options, new StringReader("operator@example.com\n"));

        Assert.True(options.ReadEmailFromStandardInput);
        Assert.Equal("operator@example.com", email);
    }

    [Fact]
    public async Task EmailInput_RejectsInvalidStandardInput()
    {
        var options = HttpsDeployOptions.Parse([
            "enable",
            "--ip", "124.222.213.245",
            "--email-stdin", "true",
            "--confirm", HttpsDeployOptions.EnableConfirmation
        ]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            AcmeEmailInput.ResolveAsync(options, new StringReader("not-an-email\n")));
    }

    [Fact]
    public async Task EmailInput_ReadsAddressFromExplicitFile()
    {
        var emailFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(emailFile, "operator@example.com\n");
            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(emailFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            var options = HttpsDeployOptions.Parse([
                "enable",
                "--ip", "124.222.213.245",
                "--email-file", Path.GetFullPath(emailFile),
                "--confirm", HttpsDeployOptions.EnableConfirmation
            ]);

            Assert.Equal("operator@example.com", await AcmeEmailInput.ResolveAsync(options));
        }
        finally
        {
            File.Delete(emailFile);
        }
    }

    [Fact]
    public void Parse_RejectsMultipleEmailSources()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            HttpsDeployOptions.Parse([
                "stage",
                "--ip", "124.222.213.245",
                "--email", "operator@example.com",
                "--email-stdin", "true",
                "--confirm", HttpsDeployOptions.StageConfirmation
            ]));

        Assert.Contains("exactly one", exception.Message);
    }

    [Fact]
    public void NginxConfig_CoversTlsRedirectApiDatabaseAndSignalR()
    {
        var config = NginxConfigRenderer.Render("124.222.213.245");

        Assert.Contains("return 308 https://$host$request_uri", config);
        Assert.Contains("ssl_protocols TLSv1.2 TLSv1.3", config);
        Assert.Contains("location /api/", config);
        Assert.Contains("location = /health/database", config);
        Assert.Contains("location /hubs/", config);
        Assert.Contains("proxy_set_header Upgrade $http_upgrade", config);
        Assert.Contains("/etc/letsencrypt/live/steam-platform-ip/fullchain.pem", config);
        Assert.DoesNotContain("1521", config);
        Assert.DoesNotContain("TLSv1.1", config);
    }

    [Fact]
    public void RenewalUnits_TargetOnlyProductionCertificateAndReloadThroughDeployHook()
    {
        var service = SystemdUnitRenderer.RenderRenewalService();
        var timer = SystemdUnitRenderer.RenderRenewalTimer();

        Assert.Contains("Environment=PYTHONPATH=/opt/steam-platform/tools/certbot/packages", service);
        Assert.Contains("renew --quiet --cert-name steam-platform-ip", service);
        Assert.DoesNotContain("ExecStartPost", service);
        Assert.Contains("OnCalendar=hourly", timer);
        Assert.Contains("RandomizedDelaySec=15m", timer);
    }

    [Fact]
    public void DeploymentState_SurvivesJsonAuditRoundTrip()
    {
        var original = new DeploymentState(
            "124.222.213.245",
            "/etc/nginx/sites-available/steam-platform.pre-https-20260827180000.bak",
            "/etc/nginx/sites-enabled/steam-platform.pre-https-20260827180000.bak",
            DateTimeOffset.Parse("2026-08-27T10:00:00Z"),
            SystemCertbotTimerEnabled: true,
            SystemCertbotTimerActive: true);

        var restored = JsonSerializer.Deserialize<DeploymentState>(JsonSerializer.Serialize(original));

        Assert.Equal(original, restored);
    }

    [Fact]
    public void LoopbackProbeClient_PreservesExplicitRedirectPolicy()
    {
        using var client = LoopbackHttpClientFactory.Create(allowAutoRedirect: false, TimeSpan.FromSeconds(7));

        Assert.Equal(TimeSpan.FromSeconds(7), client.Timeout);
    }

    [Fact]
    public void BackupPaths_NeverEnterNginxIncludeDirectories()
    {
        var paths = DeploymentBackupPaths.Create("20260827180000");

        Assert.StartsWith("/var/lib/steam-platform-https/backups/", paths.AvailableConfig);
        Assert.StartsWith("/var/lib/steam-platform-https/backups/", paths.EnabledConfig);
        Assert.DoesNotContain("/etc/nginx/sites-enabled", paths.AvailableConfig);
        Assert.DoesNotContain("/etc/nginx/sites-enabled", paths.EnabledConfig);
    }
}
