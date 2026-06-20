namespace Hhs.VideoGeneratorService.Configuration;

public sealed class ProviderEndpointsOptions
{
    public string StorageBaseUrl { get; set; } = string.Empty;

    public AudioProviderEndpoints AudioProviders { get; set; } = new();
    public VideoProviderEndpoints VideoProviders { get; set; } = new();
}

public sealed class AudioProviderEndpoints
{
    public string QuickBaseUrl { get; set; } = string.Empty;
    public string HQBaseUrl { get; set; } = string.Empty;
}

public sealed class VideoProviderEndpoints
{
    public string FastExternalBaseUrl { get; set; } = string.Empty;
    public string FastInternalBaseUrl { get; set; } = string.Empty;
    public string QueueExternalBaseUrl { get; set; } = string.Empty;
    public string QueueInternalBaseUrl { get; set; } = string.Empty;
}
