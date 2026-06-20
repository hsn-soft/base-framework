using Hhs.Shared.Configuration;
using Hhs.Shared.Configuration.Providers.Audio;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class AudioQueueProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioQueue";

    public bool SendMailAfterGeneration { get; set; } = false;
}
