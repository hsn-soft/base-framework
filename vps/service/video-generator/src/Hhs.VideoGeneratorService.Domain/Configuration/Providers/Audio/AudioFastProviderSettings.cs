using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;

public sealed class AudioFastProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioFast";

    public bool SendMailAfterGeneration { get; set; } = false;
}
