namespace SteamPlatform.HttpsDeploy;

public sealed record DeploymentState(
    string PublicIp,
    string AvailableConfigBackupPath,
    string EnabledConfigBackupPath,
    DateTimeOffset EnabledAtUtc,
    bool SystemCertbotTimerEnabled,
    bool SystemCertbotTimerActive);
