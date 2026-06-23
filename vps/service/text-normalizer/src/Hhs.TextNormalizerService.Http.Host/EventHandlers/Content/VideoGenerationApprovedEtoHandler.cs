using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class VideoGenerationApprovedEtoHandler(
    IAppConsoleLogger logger,
    IContentNormalizedRequestAppService contentNormalizedRequestAppService,
    IAnalysisNormalizedRequestAppService analysisNormalizedRequestAppService
) : IIntegrationEventHandler<VideoGenerationApprovedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IContentNormalizedRequestAppService _contentNormalizedRequestAppService = contentNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(contentNormalizedRequestAppService));
    private readonly IAnalysisNormalizedRequestAppService _analysisNormalizedRequestAppService = analysisNormalizedRequestAppService ?? throw new ArgumentNullException(nameof(analysisNormalizedRequestAppService));

    public async Task HandleAsync(MessageEnvelope<VideoGenerationApprovedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(VideoGenerationApprovedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        switch (@event.Message.ReferenceContentType)
        {
            case ReferenceContentTypes.CUSTOMER_CONTENT:
                {
                    _contentNormalizedRequestAppService.SetParentIntegrationEvent(@event);
                    await _contentNormalizedRequestAppService.VideoGenerationApprovedAsync(@event.Message.ScopeKey, @event.Message.ReferenceContentId);
                    break;
                }
            case ReferenceContentTypes.ANALYSIS_CONTENT:
                {
                    _analysisNormalizedRequestAppService.SetParentIntegrationEvent(@event);
                    await _analysisNormalizedRequestAppService.VideoGenerationApprovedAsync(@event.Message.ScopeKey, @event.Message.ReferenceContentId);
                    break;
                }
            default: throw new ArgumentOutOfRangeException();
        }
    }
}