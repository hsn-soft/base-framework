using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.VideoGenerator;

public class VideoAudioStartedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationService contentOperationService
) : ApplicationEventHandlerBase<VideoAudioStartedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationService _contentOperationService = contentOperationService ?? throw new ArgumentNullException(nameof(contentOperationService));

    protected override async Task ExecuteAsync(MessageEnvelope<VideoAudioStartedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(VideoAudioStartedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationService.HandleVideoAudioStartedAsync(@event.Message, cancellationToken);
    }
}
