using Hhs.Shared.Helper.Configuration;

namespace Hhs.TextNormalizerService.Domain.Configuration;

public sealed class NormalizerRetrySettings : RetrySettingsBase
{
    /// <summary>
    /// How long (in minutes) an EventInboxMessage may stay in 'Started' status before being
    /// considered stale and reset to 'Failed' so the next broker re-delivery can process it.
    /// Default: 15 minutes.
    /// </summary>
    public int StaleInboxMessageThresholdMinutes { get; set; } = 15;
}
