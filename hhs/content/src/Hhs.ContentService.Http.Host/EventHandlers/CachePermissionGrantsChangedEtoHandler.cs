using Hhs.Shared.Contracts;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.EventBus.Logging;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers;

public sealed class CachePermissionGrantsChangedEtoHandler(IAppConsoleLogger logger) : IIntegrationEventHandler<CachePermissionGrantsChangedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task HandleAsync(MessageEnvelope<CachePermissionGrantsChangedEto> @event)
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