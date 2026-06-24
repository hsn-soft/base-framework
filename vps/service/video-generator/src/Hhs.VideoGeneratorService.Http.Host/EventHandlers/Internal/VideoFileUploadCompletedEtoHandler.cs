using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Infrastructure;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoFileUploadCompletedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : VideoEventHandlerBase<VideoFileUploadCompletedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly VideoOperationAppService _videoOperationAppService = videoOperationAppService ?? throw new ArgumentNullException(nameof(videoOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<VideoFileUploadCompletedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => EventId[{EventId}]",
            nameof(VideoFileUploadCompletedEto)[..^"Eto".Length],
            @event.MessageId);

        await _videoOperationAppService.HandleVideoUploadCompletedAsync(@event.Message, cancellationToken);
    }
}