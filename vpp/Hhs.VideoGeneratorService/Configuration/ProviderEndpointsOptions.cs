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
    public string FastBaseUrl { get; set; } = string.Empty;
    public string SyncBaseUrl { get; set; } = string.Empty;
    public string ProBaseUrl { get; set; } = string.Empty;
    public string CloudBaseUrl { get; set; } = string.Empty;
}
