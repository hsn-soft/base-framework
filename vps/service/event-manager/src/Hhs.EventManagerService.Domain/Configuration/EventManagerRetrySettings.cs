using Hhs.Shared.Helper.Configuration;

namespace Hhs.EventManagerService.Domain.Configuration;

public sealed class EventManagerRetrySettings : RetrySettingsBase
{
    /// <summary>
    /// Ceiling on how many times a single logical event may be manually re-queued via
    /// ReQueueFailedEventByIdAsync. Distinct from <see cref="RetrySettingsBase.MaxRetryCount"/>,
    /// which only bounds broker-level redelivery of the SAME message id — every manual requeue
    /// mints a brand-new message id (see ReQueuedEtoHandler.BuildOriginalEnvelope), so without this
    /// separate ceiling a permanently-broken event could be requeued indefinitely with no circuit
    /// breaker. Checked against FailedIntegrationEvent.ReQueuedCount, which is carried forward
    /// across requeue generations via the message envelope's ReQueuedCount.
    /// </summary>
    public int MaxReQueueCount { get; set; } = 4;
}
