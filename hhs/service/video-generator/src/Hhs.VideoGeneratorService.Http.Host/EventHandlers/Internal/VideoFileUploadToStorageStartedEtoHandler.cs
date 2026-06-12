using Hhs.VideoGeneratorService.Application.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Contracts.VideoDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoFileUploadToStorageStartedEtoHandler : IIntegrationEventHandler<VideoFileUploadToStorageStartedEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly IVideoOperationsAppService _videoOperationsAppService;

    public VideoFileUploadToStorageStartedEtoHandler(IAppConsoleLogger logger,
        IVideoOperationsAppService videoOperationsAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _videoOperationsAppService = videoOperationsAppService ?? throw new ArgumentNullException(nameof(videoOperationsAppService));
    }

    public async Task HandleAsync(MessageEnvelope<VideoFileUploadToStorageStartedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(VideoFileUploadToStorageStartedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _videoOperationsAppService.SetParentIntegrationEvent(@event);
        await _videoOperationsAppService.VideoFileUploadToStorageAsync(@event.Message.VideoRequestId);
    }
}