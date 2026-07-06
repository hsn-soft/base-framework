using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoFileUploadCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : ApplicationEventHandlerBase<VideoFileUploadCompletedEto>(inboxStore, logger, videoOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<VideoFileUploadCompletedEto> @event, CancellationToken cancellationToken)
        => await videoOperationAppService.HandleVideoUploadCompletedAsync(@event.Message, cancellationToken);
}