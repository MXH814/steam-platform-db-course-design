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
            DateTimeOffset.Parse("2026-08-27T10:00:00Z"),
            SystemCertbotTimerEnabled: true,
            SystemCertbotTimerActive: true);

        var restored = JsonSerializer.Deserialize<DeploymentState>(JsonSerializer.Serialize(original));

        Assert.Equal(original, restored);
    }
}
