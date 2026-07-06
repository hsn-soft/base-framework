using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Services;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.TextNormalizerService.EventHandlers.Content;

public class VideoGenerationApprovedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    NormalizerOperationAppService normalizerOperationAppService
) : ApplicationEventHandlerBase<VideoGenerationApprovedEto>(inboxStore, logger, normalizerOperationAppService)
{
    protected override async Task ExecuteAsync(MessageEnvelope<VideoGenerationApprovedEto> @event, CancellationToken cancellationToken)
        => await normalizerOperationAppService.ForwardVideoGenerationDataAsync(@event.Message, cancellationToken);
}