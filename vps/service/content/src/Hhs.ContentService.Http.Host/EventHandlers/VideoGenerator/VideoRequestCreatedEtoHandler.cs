using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.VideoGenerator;

public class VideoRequestCreatedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationAppService contentOperationAppService
) : ApplicationEventHandlerBase<VideoRequestCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationAppService _contentOperationAppService = contentOperationAppService ?? throw new ArgumentNullException(nameof(contentOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<VideoRequestCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(VideoRequestCreatedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationAppService.HandleVideoRequestCreatedAsync(@event.Message.RefContentId, @event.Message.VideoRequestId, @event.Message.RefContentType, cancellationToken);
    }
}
