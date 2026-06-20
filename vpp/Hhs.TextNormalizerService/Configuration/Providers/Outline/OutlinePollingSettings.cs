using Hhs.Shared.Configuration;

namespace Hhs.TextNormalizerService.Configuration.Providers.Outline;

public sealed class OutlinePollingSettings : PollingSettings
{
    public const string SectionName = "Polling:Outline";

    public int ImmediateRescheduleDelaySeconds { get; set; } = 5;
}
