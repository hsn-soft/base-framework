using Hhs.Shared.Helper.Configuration;

namespace Hhs.VideoGeneratorService.Domain.Configuration;

public sealed class VideoPollingSettings : PollingSettings
{
    public const string SectionName = "Polling:Video";
}
