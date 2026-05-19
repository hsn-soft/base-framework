using HsnSoft.Base.Tracing;

namespace Hhs.Shared.Hosting.Helpers;

public static class AppInfoResolver
{
    public static void Resolve(Type type)
    {
        ApplicationIdentifier.AppId = AppIdResolve();

        string workspace = type.Namespace ?? "Application";
        ApplicationIdentifier.AppName = workspace.Contains('.') ? workspace[(workspace.IndexOf('.') + 1)..] : workspace;
    }

    private static string AppIdResolve()
    {
        // 1. External override
        string appId = Environment.GetEnvironmentVariable("APP_ID");
        if (!string.IsNullOrWhiteSpace(appId))
            return appId;

        // 2. Kubernetes
        string podName = Environment.GetEnvironmentVariable("POD_NAME");
        if (!string.IsNullOrWhiteSpace(podName))
            return podName;

        // 3. Docker
        string hostName = Environment.GetEnvironmentVariable("HOSTNAME");
        if (!string.IsNullOrWhiteSpace(hostName))
            return hostName;

        // 4. fallback
        return Guid.NewGuid().ToString("N");
    }
}