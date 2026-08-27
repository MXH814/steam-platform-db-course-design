namespace SteamPlatform.HttpsDeploy;

public sealed record DeploymentState(
    string PublicIp,
    string BackupPath,
    DateTimeOffset EnabledAtUtc,
    bool SystemCertbotTimerEnabled,
    bool SystemCertbotTimerActive);
