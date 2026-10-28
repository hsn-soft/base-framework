namespace Edx.Shared.Hosting.Gateways.Options;

/// <summary>What a gateway on a public network protects itself with (section <c>GatewayPolicies</c>).</summary>
public sealed class GatewayPolicyOptions
{
    public const string SectionName = "GatewayPolicies";

    /// <summary>The proxies in front of the gateway whose <c>X-Forwarded-For</c> is believed (a load balancer, an ingress): without them the client address is the proxy's.</summary>
    public string[] TrustedProxyIps { get; set; } = [];

    public KestrelPolicyOptions Kestrel { get; set; } = new();

    /// <summary>The browsers that may call the gateway from another address (a back office in the browser): the origins that are answered. None, and no browser from another origin is.</summary>
    public CorsPolicyOptions Cors { get; set; } = new();
}

public sealed class CorsPolicyOptions
{
    /// <summary>The origins (<c>http://localhost:4200</c>), exactly as the browser sends them.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}

public sealed class KestrelPolicyOptions
{
    public long MaxRequestBodySizeBytes { get; set; } = 104857600;   // 100 MB, the hard ceiling

    public int RequestHeadersTimeoutSeconds { get; set; } = 15;

    public int KeepAliveTimeoutSeconds { get; set; } = 60;

    public int MaxRequestHeaderCount { get; set; } = 64;

    public int MaxRequestHeadersTotalSizeBytes { get; set; } = 32768;   // 32 KB
}
