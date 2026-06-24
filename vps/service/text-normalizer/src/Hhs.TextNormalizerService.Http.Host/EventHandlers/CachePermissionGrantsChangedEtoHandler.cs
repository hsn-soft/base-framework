using Hhs.Shared.Contracts;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Infrastructure;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers;

public sealed class CachePermissionGrantsChangedEtoHandler(
    IAppConsoleLogger logger,
    NormalizerInboxStoreService inboxStore
) : NormalizerEventHandlerBase<CachePermissionGrantsChangedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(MessageEnvelope<CachePermissionGrantsChangedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}]",
            @event.Producer,
            nameof(CachePermissionGrantsChangedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty);

        // Simulate a work time
        await Task.Delay(1000, cancellationToken);

        // Synch Permission Service Store background job trigger flag active
        BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore = true;

        await Task.CompletedTask;
    }
}