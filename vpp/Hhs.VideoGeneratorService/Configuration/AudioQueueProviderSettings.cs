using Hhs.Shared.Configuration;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class AudioQueueProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioQueue";

    public bool SendMailAfterGeneration { get; set; } = false;
}
