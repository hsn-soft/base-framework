using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class AudioFileDownloadStartedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : ApplicationEventHandlerBase<AudioFileDownloadStartedEto>(inboxStore, logger, videoOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<AudioFileDownloadStartedEto> @event, CancellationToken cancellationToken)
        => await videoOperationAppService.DownloadAudioFileAsync(@event.Message, cancellationToken);
}