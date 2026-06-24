using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Infrastructure;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class AudioFileDownloadCompletedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : VideoEventHandlerBase<AudioFileDownloadCompletedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly VideoOperationAppService _videoOperationAppService = videoOperationAppService ?? throw new ArgumentNullException(nameof(videoOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<AudioFileDownloadCompletedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => EventId[{EventId}]",
            nameof(AudioFileDownloadCompletedEto)[..^"Eto".Length],
            @event.MessageId);

        await _videoOperationAppService.HandleAudioDownloadCompletedAsync(@event.Message, cancellationToken);
    }
}