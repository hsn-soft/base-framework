using Hhs.Shared.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Configuration.Providers.Audio;

public sealed class AudioFastProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioFast";

    public bool SendMailAfterGeneration { get; set; } = false;
}
