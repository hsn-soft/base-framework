using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.Shared.Contracts.Events.Content;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AppContentNormalizedRequestCreatedEtoHandler : IIntegrationEventHandler<AppContentNormalizedRequestCreatedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly IAppContentAppService _appContentAppService;

    public AppContentNormalizedRequestCreatedEtoHandler(IAppConsoleLogger logger,
        IAppContentAppService appContentAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appContentAppService = appContentAppService ?? throw new ArgumentNullException(nameof(appContentAppService));
    }

    public async Task HandleAsync(MessageEnvelope<AppContentNormalizedRequestCreatedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(AppContentNormalizedRequestCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _appContentAppService.SetParentIntegrationEvent(@event);
        await _appContentAppService.SetNormalizedRequestReferenceAsync(@event.Message.AppContentId, @event.Message.NormalizedRequestId);
    }
}