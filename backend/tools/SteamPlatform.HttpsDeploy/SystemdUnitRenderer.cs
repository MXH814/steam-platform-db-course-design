namespace SteamPlatform.HttpsDeploy;

public static class SystemdUnitRenderer
{
    public static string RenderRenewalService() => """
        [Unit]
        Description=Renew Steam Platform short-lived Let's Encrypt certificate
        After=network-online.target
        Wants=network-online.target

        [Service]
        Type=oneshot
        Environment=PYTHONPATH=/opt/steam-platform/tools/certbot/packages
        ExecStart=/usr/bin/python3 -c "from certbot.main import main; raise SystemExit(main())" renew --quiet --cert-name steam-platform-ip
        """ + Environment.NewLine;

    public static string RenderRenewalTimer() => """
        [Unit]
        Description=Check Steam Platform short-lived certificate renewal hourly

        [Timer]
        OnCalendar=hourly
        RandomizedDelaySec=15m
        Persistent=true

        [Install]
        WantedBy=timers.target
        """ + Environment.NewLine;
}
