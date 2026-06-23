namespace Hhs.Gateway.Commercial.Options.Docs;

public sealed class GatewayServiceLiveInfo
{
    public SwaggerServiceDefinition Definition { get; set; } = new();

    public EndpointProbeResult? ServiceHealth { get; set; }

    public EndpointProbeResult? GatewayHealth { get; set; }

    public EndpointProbeResult? ServiceRoot { get; set; }

    public EndpointProbeResult? GatewayRoot { get; set; }
}