using Hhs.Shared.Contracts;
using Hhs.Shared.Contracts.Events;
using Hhs.IdentityService.Application.Infrastructure;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.IdentityService.EventHandlers;

public sealed class CachePermissionGrantsChangedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger
) : ApplicationEventHandlerBase<CachePermissionGrantsChangedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(MessageEnvelope<CachePermissionGrantsChangedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("{Producer} Event[ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            @event.Producer,
            nameof(CachePermissionGrantsChangedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        // Simulate a work time
        await Task.Delay(1000);

        // Synch Permission Service Store background job trigger flag active
        BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore = true;

        await Task.CompletedTask;
    }
}