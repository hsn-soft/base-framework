using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class TrendVideoGenerationQueryEtoHandler : IIntegrationEventHandler<TrendVideoGenerationQueryEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly ICustomerContentAppService _appContentAppService;

    public TrendVideoGenerationQueryEtoHandler(IAppConsoleLogger logger,
        ICustomerContentAppService appContentAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appContentAppService = appContentAppService ?? throw new ArgumentNullException(nameof(appContentAppService));
    }

    public async Task HandleAsync(MessageEnvelope<TrendVideoGenerationQueryEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(TrendVideoGenerationQueryEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _appContentAppService.SetParentIntegrationEvent(@event);
        await _appContentAppService.TrendVideoGenerationQueryAsync(@event.Message);
    }
}