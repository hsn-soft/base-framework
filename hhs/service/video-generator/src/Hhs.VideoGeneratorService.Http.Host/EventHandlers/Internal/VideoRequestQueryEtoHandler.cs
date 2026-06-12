using Hhs.VideoGeneratorService.Application.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Contracts.VideoDomain.Interfaces;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoRequestQueryEtoHandler : IIntegrationEventHandler<VideoRequestQueryEto>
{
    private readonly IAppConsoleLogger _logger;
    private readonly IVideoOperationsAppService _videoOperationsAppService;

    public VideoRequestQueryEtoHandler(IAppConsoleLogger logger,
        IVideoOperationsAppService videoOperationsAppService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _videoOperationsAppService = videoOperationsAppService ?? throw new ArgumentNullException(nameof(videoOperationsAppService));
    }

    public async Task HandleAsync(MessageEnvelope<VideoRequestQueryEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(VideoRequestQueryEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        _videoOperationsAppService.SetParentIntegrationEvent(@event);
        await _videoOperationsAppService.VideoRequestQueryAsync(@event.Message);
    }
}