using Hhs.ContentService.Application.Infrastructure;
using Hhs.Shared.Contracts;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers;

public sealed class CachePermissionGrantsChangedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger
) : ContentEventHandlerBase<CachePermissionGrantsChangedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(MessageEnvelope<CachePermissionGrantsChangedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Event[ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(CachePermissionGrantsChangedEto)[..^"Eto".Length],
            @event.MessageId);

        // Simulate a work time
        await Task.Delay(1000, cancellationToken);

        // Synch Permission Service Store background job trigger flag active
        BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore = true;

        await Task.CompletedTask;
    }
}