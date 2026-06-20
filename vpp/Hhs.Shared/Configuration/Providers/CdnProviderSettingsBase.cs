namespace Hhs.Shared.Configuration.Providers;

public class CdnProviderSettingsBase : IHasCdnBaseUrl
{
    // Base CDN Settings (Common to all CDN providers)
    public string BaseUrl { get; set; } = default!;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string ZonePath { get; set; } = "media";
    public string PathPrefix { get; set; } = "prod";
    public object? Storage { get; set; }
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
