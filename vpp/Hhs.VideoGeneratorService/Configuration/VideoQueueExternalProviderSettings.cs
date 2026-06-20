using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers.Video;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class VideoQueueExternalProviderSettings : VideoProviderSettingsBase
{
    public const string SectionName = "Provider:Video:VideoQueueExternal";
}
