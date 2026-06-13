using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Internal;

public class SetNormalizedStatusToFailedEtoHandler(
    IAppConsoleLogger logger,
    IContentNormalizedRequestAppService contentNormalizedRequestAppService,
    IAnalysisNormalizedRequestAppService analysisNormalizedRequestAppService
) : IIntegrationEventHandler<SetNormalizedStatusToFailedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IContentNormalizedRequestAppService _contentNormalizedRequestAppService = contentNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(contentNormalizedRequestAppService));
    private readonly IAnalysisNormalizedRequestAppService _analysisNormalizedRequestAppService = analysisNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(analysisNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<SetNormalizedStatusToFailedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(SetNormalizedStatusToFailedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        switch (@event.Message.ReferenceNormalizedType)
        {
            case ReferenceContentTypes.CUSTOMER_CONTENT:
                {
                    _contentNormalizedRequestAppService.SetParentIntegrationEvent(@event);
                    await _contentNormalizedRequestAppService.SetStatusToFailedAsync(@event.Message.ReferenceNormalizedId, @event.Message.FailedReason, @event.CorrelationId);
                    break;
                }
            case ReferenceContentTypes.ANALYSIS_CONTENT:
                {
                    _analysisNormalizedRequestAppService.SetParentIntegrationEvent(@event);
                    await _analysisNormalizedRequestAppService.SetStatusToFailedAsync(@event.Message.ReferenceNormalizedId, @event.Message.FailedReason, @event.CorrelationId);
                    break;
                }
            default: throw new ArgumentOutOfRangeException();
        }
    }
}