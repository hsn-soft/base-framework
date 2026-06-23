using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.Internal;

public class SetContentStatusToFailedEtoHandler(
    IAppConsoleLogger logger,
    ICustomerContentAppService customerContentAppService,
    IAnalysisContentAppService analysisContentAppService
) : IIntegrationEventHandler<SetContentStatusToFailedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICustomerContentAppService _customerContentAppService = customerContentAppService ?? throw new ArgumentNullException(nameof(customerContentAppService));
    private readonly IAnalysisContentAppService _analysisContentAppService = analysisContentAppService ?? throw new ArgumentNullException(nameof(analysisContentAppService));

    public async Task HandleAsync(MessageEnvelope<SetContentStatusToFailedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(SetContentStatusToFailedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);

        switch (@event.Message.ReferenceContentType)
        {
            case ReferenceContentTypes.CUSTOMER_CONTENT:
                {
                    _customerContentAppService.SetParentIntegrationEvent(@event);
                    await _customerContentAppService.SetCustomerContentStatusToFailedAsync(@event.Message.ReferenceContentId, @event.Message.FailedReason, @event.CorrelationId);
                    break;
                }
            case ReferenceContentTypes.ANALYSIS_CONTENT:
                {
                    _analysisContentAppService.SetParentIntegrationEvent(@event);
                    await _analysisContentAppService.SetAnalysisContentStatusToFailedAsync(@event.Message.ReferenceContentId, @event.Message.FailedReason, @event.CorrelationId);
                    break;
                }
            default: throw new ArgumentOutOfRangeException();
        }
    }
}