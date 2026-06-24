using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Infrastructure;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoOperationStartedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : VideoEventHandlerBase<VideoOperationStartedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly VideoOperationAppService _videoOperationAppService = videoOperationAppService ?? throw new ArgumentNullException(nameof(videoOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<VideoOperationStartedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => EventId[{EventId}]",
            nameof(VideoOperationStartedEto)[..^"Eto".Length],
            @event.MessageId);

        await _videoOperationAppService.HandleVideoOperationStartedAsync(@event.Message, @event.MessageId, cancellationToken);
    }
}