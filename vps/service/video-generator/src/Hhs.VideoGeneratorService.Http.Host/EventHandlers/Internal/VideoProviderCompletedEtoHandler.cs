using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoProviderCompletedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : ApplicationEventHandlerBase<VideoProviderCompletedEto>(inboxStore, logger, videoOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<VideoProviderCompletedEto> @event, CancellationToken cancellationToken)
        => await videoOperationAppService.HandleVideoProviderCompletedAsync(@event.Message, cancellationToken);
}