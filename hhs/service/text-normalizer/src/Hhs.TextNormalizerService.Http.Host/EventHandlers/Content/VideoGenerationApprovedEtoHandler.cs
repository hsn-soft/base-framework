using Hhs.Shared.Contracts.Events.TextNormalizer;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class VideoGenerationApprovedEtoHandler : IIntegrationEventHandler<VideoGenerationApprovedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly INormalizedRequestAppService _normalizedRequestAppService;
    private readonly INormalizedAnalysisAppService _normalizedAnalysisAppService;

    public VideoGenerationApprovedEtoHandler(IAppConsoleLogger logger,
        INormalizedRequestAppService normalizedRequestAppService,
        INormalizedAnalysisAppService normalizedAnalysisAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _normalizedRequestAppService = normalizedRequestAppService ?? throw new ArgumentNullException(nameof(normalizedRequestAppService));
        _normalizedAnalysisAppService = normalizedAnalysisAppService ?? throw new ArgumentNullException(nameof(normalizedAnalysisAppService));
    }

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
                _normalizedRequestAppService.SetParentIntegrationEvent(@event);
                await _normalizedRequestAppService.VideoGenerationApprovedAsync(@event.Message);
                break;
            }
            case ReferenceContentTypes.ANALYSIS_CONTENT:
            {
                _normalizedAnalysisAppService.SetParentIntegrationEvent(@event);
                await _normalizedAnalysisAppService.VideoGenerationApprovedAsync(@event.Message);
                break;
            }
            default: throw new ArgumentOutOfRangeException();
        }
    }
}