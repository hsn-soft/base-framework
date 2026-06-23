using Hhs.Shared.Helper.Configuration;

namespace Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;

public sealed class OutlinePollingSettings : PollingSettings
{
    public const string SectionName = "Polling:Outline";

    public int ImmediateRescheduleDelaySeconds { get; set; } = 5;
}
