using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class SetNormalizedStatusToFailedEtoHandler : IIntegrationEventHandler<SetNormalizedStatusToFailedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly INormalizedRequestAppService _normalizedRequestAppService;
    private readonly INormalizedAnalysisAppService _normalizedAnalysisAppService;

    public SetNormalizedStatusToFailedEtoHandler(IAppConsoleLogger logger,
        INormalizedRequestAppService normalizedRequestAppService,
        INormalizedAnalysisAppService normalizedAnalysisAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizedRequestAppService = normalizedRequestAppService ?? throw new ArgumentNullException(nameof(normalizedRequestAppService));
        _normalizedAnalysisAppService = normalizedAnalysisAppService ?? throw new ArgumentNullException(nameof(normalizedAnalysisAppService));
    }

    public async Task HandleAsync(MessageEnvelope<SetNormalizedStatusToFailedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(SetNormalizedStatusToFailedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        switch (@event.Message.ReferenceNormalizedType)
        {
            case ReferenceContentTypes.APP_REQUEST_CONTENT:
            {
                _normalizedRequestAppService.SetParentIntegrationEvent(@event);
                await _normalizedRequestAppService.SetStatusToFailedAsync(@event.Message.ReferenceNormalizedId, @event.Message.FailedReason, @event.CorrelationId);
                break;
            }
            case ReferenceContentTypes.ANALYSIS_CONTENT:
            {
                _normalizedAnalysisAppService.SetParentIntegrationEvent(@event);
                await _normalizedAnalysisAppService.SetStatusToFailedAsync(@event.Message.ReferenceNormalizedId, @event.Message.FailedReason, @event.CorrelationId);
                break;
            }
            default: throw new ArgumentOutOfRangeException();
        }
    }
}