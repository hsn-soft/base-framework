using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.Shared.Contracts.Events.Content;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AppContentNormalizedResultEtoHandler : IIntegrationEventHandler<AppContentNormalizedResultEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly ICustomerContentAppService _appContentAppService;

    public AppContentNormalizedResultEtoHandler(IAppConsoleLogger logger,
        ICustomerContentAppService appContentAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appContentAppService = appContentAppService ?? throw new ArgumentNullException(nameof(appContentAppService));
    }

    public async Task HandleAsync(MessageEnvelope<AppContentNormalizedResultEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AppContentNormalizedResultEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _appContentAppService.SetParentIntegrationEvent(@event);
        await _appContentAppService.SetCustomerContentNormalizedResultAsync(@event.Message.AppContentId, @event.Message.NormalizedRequestId,
            @event.Message.IsNormalizedSuccess, @event.Message.ReleaseTime, @event.CorrelationId);
    }
}