namespace Hhs.TextNormalizerService.Configuration;

public sealed class OutlineProviderEndpointsOptions
{
    public string FastBaseUrl { get; set; } = "http://localhost:5040";
    public string DetailedBaseUrl { get; set; } = "http://localhost:5041";
}
