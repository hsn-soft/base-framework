using System.Security.Claims;
using Hhs.Gateway.Commercial.Options;

namespace Hhs.Gateway.Commercial.Helpers;

public enum GatewayRateCategory
{
    Docs,
    ContentSearch,
    Default
}

public static class GatewayRouteClassifier
{
    public static GatewayRateCategory GetRateCategory(HttpContext context, GatewayPolicyOptions options)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (MatchesAnyPrefix(path, options.Routes.DocsPrefixes))
            return GatewayRateCategory.Docs;

        if (MatchesAnyPrefix(path, options.Routes.ContentSearchPrefixes))
            return GatewayRateCategory.ContentSearch;

        return GatewayRateCategory.Default;
    }

    public static long GetMaxRequestBodySize(HttpContext context, GatewayPolicyOptions options)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsDelete(method))
            return options.RequestBodyLimits.DownloadBytes;

        if (MatchesAnyPrefix(path, options.Routes.ContentSearchPrefixes))
            return options.RequestBodyLimits.ContentBytes;

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

    public static bool MatchesAnyPrefix(string path, IEnumerable<string> prefixes)
        => prefixes.Any(p => !string.IsNullOrWhiteSpace(p) &&
                             path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    public static bool MatchesAnyExact(string path, IEnumerable<string> exactPaths)
        => exactPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
}