namespace Hhs.Gateway.Commercial.Options;

public sealed class ServiceEndpointRegistryOptions
{
    public Dictionary<string, ServiceEndpointItem> Services { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ServiceEndpointItem
{
    public string Origin { get; set; } = string.Empty;
}