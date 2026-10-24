namespace Abc.Shared.Hosting.Gateways.Options;

/// <summary>The services behind a gateway (section <c>ServiceEndpoints</c>), by key (<c>identity-service</c>).</summary>
public sealed class ServiceEndpointRegistryOptions
{
    public Dictionary<string, ServiceEndpointItem> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ServiceEndpointItem
{
    /// <summary>The internal address of the service (<c>http://identity:8080</c>).</summary>
    public string Origin { get; set; } = string.Empty;
}
