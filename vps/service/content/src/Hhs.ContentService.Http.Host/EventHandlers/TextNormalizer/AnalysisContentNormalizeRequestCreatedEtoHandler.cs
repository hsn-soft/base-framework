using Hhs.ContentService.Application.Infrastructure;
using Hhs.ContentService.Application.Services;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.TextNormalizer;

public class AnalysisContentNormalizeRequestCreatedEtoHandler(
    ApplicationEventInboxMessageManager inboxStore,
    IAppConsoleLogger logger,
    ContentOperationAppService contentOperationAppService
) : ApplicationEventHandlerBase<AnalysisContentNormalizeRequestCreatedEto>(inboxStore)
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ContentOperationAppService _contentOperationAppService = contentOperationAppService ?? throw new ArgumentNullException(nameof(contentOperationAppService));

    protected override async Task ExecuteAsync(MessageEnvelope<AnalysisContentNormalizeRequestCreatedEto> @event, CancellationToken cancellationToken)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}]",
            nameof(AnalysisContentNormalizeRequestCreatedEto)[..^"Eto".Length],
            @event.MessageId);

        await _contentOperationAppService.HandleNormalizeStartedAsync(@event.Message.AnalysisContentId, @event.Message.NormalizeRequestId, ContentType.AnalysisContent, cancellationToken);
    }
}
