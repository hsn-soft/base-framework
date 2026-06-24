using Hhs.Shared.Helper.Configuration;

namespace Hhs.VideoGeneratorService.Domain.Configuration;

public sealed class AudioPollingSettings : PollingSettings
{
    public const string SectionName = "Polling:Audio";
}
