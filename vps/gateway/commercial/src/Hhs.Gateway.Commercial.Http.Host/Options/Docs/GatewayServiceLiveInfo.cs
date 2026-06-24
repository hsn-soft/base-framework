using JetBrains.Annotations;

namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class GatewayServiceLiveInfo
{
    public SwaggerServiceDefinition Definition { get; set; } = new();

    [CanBeNull] public EndpointProbeResult ServiceHealth { get; set; }

    [CanBeNull] public EndpointProbeResult GatewayHealth { get; set; }

    [CanBeNull] public EndpointProbeResult ServiceRoot { get; set; }

    [CanBeNull] public EndpointProbeResult GatewayRoot { get; set; }
}