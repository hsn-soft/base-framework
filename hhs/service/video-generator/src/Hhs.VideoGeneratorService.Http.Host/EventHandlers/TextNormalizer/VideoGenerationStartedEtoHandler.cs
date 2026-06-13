using Hhs.Shared.Contracts.Events.VideoGenerator;
using Hhs.VideoGeneratorService.Application.Contracts.VideoDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.TextNormalizer;

public class VideoGenerationStartedEtoHandler(
    IAppConsoleLogger logger,
    IVideoOperationsAppService videoOperationsAppService
    ) : IIntegrationEventHandler<VideoGenerationStartedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IVideoOperationsAppService _videoOperationsAppService = videoOperationsAppService ?? throw new ArgumentNullException(nameof(videoOperationsAppService));

    public async Task HandleAsync(MessageEnvelope<VideoGenerationStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(VideoGenerationStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _videoOperationsAppService.SetParentIntegrationEvent(@event);
        await _videoOperationsAppService.VideoRequestCreateAsync(@event.Message, @event.CorrelationId);
    }
}