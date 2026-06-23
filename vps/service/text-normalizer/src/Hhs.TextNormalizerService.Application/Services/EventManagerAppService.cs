using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Contracts;
using Hhs.TextNormalizerService.Application.Contracts.Events;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class EventManagerAppService : ApplicationServiceBase, IEventManagerAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly ushort _reQueuedLimit;

    public EventManagerAppService(IServiceProvider provider) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _reQueuedLimit = 1;
    }

    public async Task EventReQueuedAsync(MessageEnvelope<ReQueuedEto> @event)
    {
        bool skipReQueueOperation = ParentIntegrationEvent.ReQueuedCount > _reQueuedLimit;

        #region Re-Generate Integration Event Model for Re-Publish

        string objectSerializedContent = @event?.Message?.ReQueuedMessageObject.ToString();

        var refType = typeof(IIntegrationEventMessage);
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany
            (
                x => x.GetTypes().Where(p => refType.IsAssignableFrom(p) && p is { IsInterface: false, IsAbstract: false })
            )
            .ToList();

        var eventType = allTypes.FirstOrDefault(x => x.Name.Equals(@event?.Message?.ReQueuedMessageTypeName));
        object originalEvent = JsonConvert.DeserializeObject(objectSerializedContent, eventType);

        #endregion

        bool isFoundTriggerEvent = true;
        switch (@event?.Message?.ReQueuedMessageTypeName)
        {
            case nameof(ContentNormalizedRequestScrapingStartedEto):
            {
                if (skipReQueueOperation)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new SetNormalizedStatusToFailedEto(
                        ReferenceNormalizedType: ReferenceContentTypes.CUSTOMER_CONTENT,
                        ReferenceNormalizedId: ((dynamic)originalEvent)?.NormalizedRequestId,
                        FailedReason: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED + ": ReQueue Limit Error")
                    );
                }
                else
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: originalEvent as ContentNormalizedRequestScrapingStartedEto);
                }

                break;
            }
            case nameof(ContentNormalizedRequestOutlineStartedEto):
            {
                if (skipReQueueOperation)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new SetNormalizedStatusToFailedEto(
                        ReferenceNormalizedType: ReferenceContentTypes.CUSTOMER_CONTENT,
                        ReferenceNormalizedId: ((dynamic)originalEvent)?.NormalizedRequestId,
                        FailedReason: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED + ": ReQueue Limit Error")
                    );
                }
                else
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: originalEvent as ContentNormalizedRequestOutlineStartedEto);
                }

                break;
            }
            case nameof(AnalysisNormalizedRequestOutlineStartedEto):
            {
                if (skipReQueueOperation)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new SetNormalizedStatusToFailedEto(
                        ReferenceNormalizedType: ReferenceContentTypes.ANALYSIS_CONTENT,
                        ReferenceNormalizedId: ((dynamic)originalEvent)?.NormalizedAnalysisId,
                        FailedReason: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED + ": ReQueue Limit Error")
                    );
                }
                else
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: originalEvent as AnalysisNormalizedRequestOutlineStartedEto);
                }

                break;
            }
            default:
            {
                isFoundTriggerEvent = false;
                break;
            }
        }

        if (!isFoundTriggerEvent || skipReQueueOperation)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Re-Queued Event Error: {@event?.Message?.ReQueuedMessageTypeName} {(skipReQueueOperation ? "ReQueue Limit Error" : "Not Implemented")}",
                reference: new { Consumer = @event?.Message?.ReQueuedMessageEnvelopeConsumer, ConsumerMessageType = @event?.Message?.ReQueuedMessageTypeName },
                facility: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED,
                correlationId: ParentIntegrationEvent?.CorrelationId,
                exception: null
            ));
        }
        else
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Re-Queued Event Success",
                reference: new { Consumer = @event?.Message?.ReQueuedMessageEnvelopeConsumer, ConsumerMessageType = @event?.Message?.ReQueuedMessageTypeName },
                facility: EventManagerOperationFacilities.EVENT_REQUEUED_SUCCESS,
                correlationId: ParentIntegrationEvent?.CorrelationId,
                exception: null
            ));
        }
    }
}