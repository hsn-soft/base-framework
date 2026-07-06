using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.VideoGenerator;

public class VideoRequestCreatedEtoHandler(
    IEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<VideoRequestCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationService _contentOperationService = contentOperationService ?? throw new ArgumentNullException(nameof(contentOperationService));

    protected override async Task ExecuteAsync(MessageEnvelope<VideoRequestCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(VideoRequestCreatedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationService.HandleVideoRequestCreatedAsync(@event.Message.RefContentId, @event.Message.VideoRequestId, @event.Message.RefContentType, @event.CorrelationId, cancellationToken);
    }
}
