using JetBrains.Annotations;

namespace Hhs.Gateway.Commercial.Options;

public sealed class ReverseProxyRouteRegistryOptions
{
    public Dictionary<string, ReverseProxyRouteItem> Routes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ReverseProxyRouteItem
{
    public string ClusterId { get; set; } = string.Empty;
    public string MatchPath { get; set; } = string.Empty;
    public string DownstreamPathPattern { get; set; } = string.Empty;
    [CanBeNull] public string ActivityTimeout { get; set; }
}