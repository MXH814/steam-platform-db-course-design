using SteamPlatform.HttpsDeploy;

try
{
    var options = HttpsDeployOptions.Parse(args);
    var service = new HttpsDeploymentService(new ProcessRunner());

    switch (options.Command)
    {
        case "plan":
            service.PrintPlan();
            break;
        case "render":
            Console.Write(NginxConfigRenderer.Render(options.PublicIp!));
            break;
        case "stage":
            await service.StageAsync(options.PublicIp!, await AcmeEmailInput.ResolveAsync(options));
            break;
        case "enable":
            await service.EnableAsync(options.PublicIp!, await AcmeEmailInput.ResolveAsync(options));
            break;
        case "verify":
            await service.VerifyAsync(options.PublicIp!);
            break;
        case "rollback":
            await service.RollbackAsync();
            break;
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    return 1;
}
