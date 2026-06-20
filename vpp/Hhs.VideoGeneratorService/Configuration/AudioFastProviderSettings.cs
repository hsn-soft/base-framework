using Hhs.Shared.Configuration;

namespace Hhs.VideoGeneratorService.Configuration;

public sealed class AudioFastProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioFast";

    public bool SendMailAfterGeneration { get; set; } = false;
}
