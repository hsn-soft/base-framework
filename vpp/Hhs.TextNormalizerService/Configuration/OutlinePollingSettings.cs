using Hhs.Shared.Configuration;

namespace Hhs.TextNormalizerService.Configuration;

public sealed class OutlinePollingSettings : PollingSettingsBase
{
    public const string SectionName = "Polling:Outline";

    public int ImmediateRescheduleDelaySeconds { get; set; } = 5;
}
