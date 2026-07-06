using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Internal;

public class VideoRequestCreatedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : ApplicationEventHandlerBase<VideoRequestCreatedEto>(inboxStore, logger, videoOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<VideoRequestCreatedEto> @event, CancellationToken cancellationToken)
        => await videoOperationAppService.StartVideoOperationAsync(@event.Message, @event.MessageId, cancellationToken);
}