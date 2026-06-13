using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.Shared.Contracts.Events.Content;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Logging.Abstracts;

namespace Hhs.ContentService.EventHandlers.VideoGenerator;

public class VideoGenerationRequestCreatedEtoHandler(
    IAppConsoleLogger logger,
    ICustomerContentAppService customerContentAppService,
    IAnalysisContentAppService analysisContentAppService
) : IIntegrationEventHandler<VideoGenerationRequestCreatedEto>
{
    private readonly IAppConsoleLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICustomerContentAppService _customerContentAppService = customerContentAppService ?? throw new ArgumentNullException(nameof(customerContentAppService));
    private readonly IAnalysisContentAppService _analysisContentAppService = analysisContentAppService ?? throw new ArgumentNullException(nameof(analysisContentAppService));

    public async Task HandleAsync(MessageEnvelope<VideoGenerationRequestCreatedEto> @event)
    {
        _logger.LogDebug("EVENT HANDLING | [ {EventName} ] => CorrelationId[{CorrelationId}], MessageId[{MessageId}], RelatedMessageId[{RelatedMessageId}]",
            nameof(VideoGenerationRequestCreatedEto)[..^"Eto".Length],
            @event.CorrelationId ?? string.Empty,
            @event.MessageId.ToString(),
            @event.ParentMessageId != null ? @event.ParentMessageId.Value.ToString() : string.Empty);


        switch (@event.Message.ReferenceContentType)
        {
            case ReferenceContentTypes.CUSTOMER_CONTENT:
                {
                    _customerContentAppService.SetParentIntegrationEvent(@event);
                    await _customerContentAppService.SetCustomerContentVideoReferenceAsync(@event.Message.ReferenceContentId, @event.Message.RefVideoRequestId);
                    break;
                }
            case ReferenceContentTypes.ANALYSIS_CONTENT:
                {
                    _analysisContentAppService.SetParentIntegrationEvent(@event);
                    await _analysisContentAppService.SetAnalysisContentVideoReferenceAsync(@event.Message.ReferenceContentId, @event.Message.RefVideoRequestId);
                    break;
                }
            default: throw new ArgumentOutOfRangeException();
        }
    }
}