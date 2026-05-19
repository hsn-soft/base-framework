using System.Security.Claims;
using Hhs.AuthServer.Options;

namespace Hhs.AuthServer.RateLimiting;

public static class ServerRouteClassifier
{
    public static ServerRateCategory GetRateCategory(HttpContext context, AuthServerPolicyOptions options)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        if (MatchesAnyExact(path, options.Routes.IdentityLoginPaths))
            return ServerRateCategory.IdentityLogin;

        if (MatchesAnyPrefix(path, options.Routes.IdentityAuthPrefixes))
            return ServerRateCategory.IdentityAuth;

        return ServerRateCategory.Default;
    }

    public static long GetMaxRequestBodySize(HttpContext context, AuthServerPolicyOptions options)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsDelete(method))
            return options.RequestBodyLimits.DownloadBytes;

        if (MatchesAnyExact(path, options.Routes.IdentityLoginPaths))
            return options.RequestBodyLimits.IdentityLoginBytes;

        if (MatchesAnyPrefix(path, options.Routes.ContentDefaultReadPrefixes)
            || MatchesAnyPrefix(path, options.Routes.ContentSearchPrefixes)
            || MatchesAnyPrefix(path, options.Routes.ContentExportPrefixes))
            return options.RequestBodyLimits.ContentBytes;

        if (MatchesAnyPrefix(path, options.Routes.TextNormalizerUploadPrefixes))
            return options.RequestBodyLimits.TextNormalizerUploadBytes;

        if (MatchesAnyPrefix(path, options.Routes.VideoGeneratorUploadPrefixes))
            return options.RequestBodyLimits.VideoUploadBytes;

        return options.RequestBodyLimits.DefaultBytes;
    }

    public static string GetPartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
            return $"user:{userId}";

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return !string.IsNullOrWhiteSpace(ip) ? $"ip:{ip}" : "ip:unknown";
    }

    private static bool MatchesAnyPrefix(string path, IEnumerable<string> prefixes)
        => prefixes.Any(p => !string.IsNullOrWhiteSpace(p) &&
                             path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesAnyExact(string path, IEnumerable<string> exactPaths)
        => exactPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
}