namespace SteamPlatform.HttpsDeploy;

public sealed record DeploymentBackupPaths(string AvailableConfig, string EnabledConfig)
{
    public const string Directory = "/var/lib/steam-platform-https/backups";

    public static DeploymentBackupPaths Create(string timestamp) =>
        new(
            $"{Directory}/available-{timestamp}.conf",
            $"{Directory}/enabled-{timestamp}.conf");
}
