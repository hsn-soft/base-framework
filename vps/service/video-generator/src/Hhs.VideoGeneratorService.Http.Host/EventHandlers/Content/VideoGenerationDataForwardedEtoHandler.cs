using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.VideoGeneratorService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.VideoGeneratorService.EventHandlers.Content;

public class VideoGenerationDataForwardedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    VideoOperationAppService videoOperationAppService
) : ApplicationEventHandlerBase<VideoGenerationDataForwardedEto>(inboxStore, logger, videoOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<VideoGenerationDataForwardedEto> @event, CancellationToken cancellationToken)
        => await videoOperationAppService.CreateVideoRequestAsync(@event.Message, @event.MessageId, @event.CorrelationId ?? "", cancellationToken);
}