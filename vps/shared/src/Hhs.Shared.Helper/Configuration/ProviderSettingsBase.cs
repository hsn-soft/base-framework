namespace Hhs.Shared.Helper.Configuration;

public abstract class ProviderSettingsBase
{
    public string BaseUrl { get; set; } = default!;
    public string? APIKey { get; set; }
}
