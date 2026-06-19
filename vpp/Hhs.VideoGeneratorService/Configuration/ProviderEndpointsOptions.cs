namespace Hhs.VideoGeneratorService.Configuration;

public sealed class ProviderEndpointsOptions
{
    public string StorageBaseUrl { get; set; } = "http://localhost:5048";

    public AudioProviderEndpoints AudioProviders { get; set; } = new();
    public VideoProviderEndpoints VideoProviders { get; set; } = new();
}

public sealed class AudioProviderEndpoints
{
    public string QuickBaseUrl { get; set; } = "http://localhost:5050";
    public string HQBaseUrl { get; set; } = "http://localhost:5051";
}

public sealed class VideoProviderEndpoints
{
    public string FastBaseUrl { get; set; } = "http://localhost:5060";
    public string SyncBaseUrl { get; set; } = "http://localhost:5063";
    public string ProBaseUrl { get; set; } = "http://localhost:5061";
    public string CloudBaseUrl { get; set; } = "http://localhost:5062";
}
