using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Cdn;

public sealed class CdnAbcSettings : CdnProviderSettingsBase
{
    public const string SectionName = "Provider:Cdn:CdnAbc";
    
    public string StorageApiBaseUrl { get; set; } = default!;
    public string StorageApiKey { get; set; } = default!;
}
