using Hhs.FeedRService.Application.Contracts.Events;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.FeedRService.Weather.EventHandlers.Internal;

public class TestQueryCompletedEtoHandler(
    IAppConsoleLogger logger,
    IRequestLimitStore limitStore) : IIntegrationEventHandler<TestQueryCompletedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRequestLimitStore _limitStore = limitStore ?? throw new ArgumentNullException(nameof(limitStore));
    private const string EndpointKey = "feedr-weather:jobs:test-query";

    public async Task HandleAsync(MessageEnvelope<TestQueryCompletedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(TestQueryCompletedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        await _limitStore.DecrementAsync(EndpointKey);
    }
}