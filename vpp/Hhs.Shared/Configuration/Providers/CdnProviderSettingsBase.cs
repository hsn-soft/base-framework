namespace Hhs.Shared.Configuration.Providers;

public class CdnProviderSettingsBase : ProviderSettingsBase
{
    public string ZonePath { get; set; } = "media";

    public string PathPrefix { get; set; } = "prod";
}
