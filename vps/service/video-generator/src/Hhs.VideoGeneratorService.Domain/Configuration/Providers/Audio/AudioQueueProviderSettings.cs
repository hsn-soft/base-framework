using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.VideoGeneratorService.Domain.Configuration.Providers.Audio;

public sealed class AudioQueueProviderSettings : AudioProviderSettingsBase
{
    public const string SectionName = "Provider:Audio:AudioQueue";

    public bool SendMailAfterGeneration { get; set; } = false;
}
