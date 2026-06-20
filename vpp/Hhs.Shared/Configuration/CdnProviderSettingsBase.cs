namespace Hhs.Shared.Configuration;

public abstract class CdnProviderSettingsBase : IHasCdnBaseUrl
{
    public string BaseUrl { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string ZoneName { get; set; } = default!;
    public string ZonePath { get; set; } = "media";
    public string PathPrefix { get; set; } = "prod";
    public StorageProviderSettingsBase Storage { get; set; } = default!;
}

/// <summary>
/// Marker interface for CDN settings that have BaseUrl, ZonePath, and PathPrefix properties.
/// </summary>
public interface IHasCdnBaseUrl
{
    string BaseUrl { get; }
    string ZonePath { get; }
    string PathPrefix { get; }
}
