using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Infrastructure;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class AudioFileDownloadStartedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : VideoEventHandlerBase<AudioFileDownloadStartedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly VideoOperationAppService _videoOperationAppService = videoOperationAppService ?? throw new ArgumentNullException(nameof(videoOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<AudioFileDownloadStartedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => EventId[{EventId}]",
            nameof(AudioFileDownloadStartedEto)[..^"Eto".Length],
            @event.MessageId);

        await _videoOperationAppService.DownloadAudioFileAsync(@event.Message, cancellationToken);
    }
}