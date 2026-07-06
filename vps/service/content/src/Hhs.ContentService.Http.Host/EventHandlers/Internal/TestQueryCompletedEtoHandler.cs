using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Contracts.EventInbox;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class TestQueryCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    IRequestLimitStore limitStore
) : ApplicationEventHandlerBase<TestQueryCompletedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRequestLimitStore _limitStore = limitStore ?? throw new ArgumentNullException(nameof(limitStore));
    private const string EndpointKey = "content:jobs:test-query";

    protected override async Task ExecuteAsync(MessageEnvelope<TestQueryCompletedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(TestQueryCompletedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        await _limitStore.DecrementAsync(EndpointKey);
    }
}
