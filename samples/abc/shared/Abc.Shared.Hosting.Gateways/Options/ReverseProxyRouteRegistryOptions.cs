namespace Abc.Shared.Hosting.Gateways.Options;

/// <summary>The routes of a gateway (section <c>ReverseProxyRoutes</c>): what a client asks for, and what the service is asked.</summary>
public sealed class ReverseProxyRouteRegistryOptions
{
    public Dictionary<string, ReverseProxyRouteItem> Routes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ReverseProxyRouteItem
{
    /// <summary>The service the route goes to: <c>identity-cluster</c> is the service <c>identity-service</c> of <c>ServiceEndpoints</c>.</summary>
    public string ClusterId { get; set; } = string.Empty;

    /// <summary>The path a client calls on the gateway (<c>/identity-service/v1/{**catch-all}</c>).</summary>
    public string MatchPath { get; set; } = string.Empty;

    /// <summary>The path the service gets (<c>/api/identity-service/v1/commercial/{**catch-all}</c>): the segment of the gateway is added here.</summary>
    public string DownstreamPathPattern { get; set; } = string.Empty;

    /// <summary>How long the service may take without sending anything (<c>00:00:30</c>).</summary>
    public string? ActivityTimeout { get; set; }
}
