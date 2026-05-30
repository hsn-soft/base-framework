using Hhs.ContentService.Application.Contracts;
using Hhs.ContentService.Application.Contracts.Events;
using Hhs.Shared.Contracts.Events.Content;
using Hhs.Shared.Helper.Consts;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Hhs.ContentService.Application.Services;

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
            case nameof(AppContentNormalizedResultEto):
            {
                if (skipReQueueOperation)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new SetContentStatusToFailedEto(
                        ReferenceContentType: ReferenceContentTypes.APP_REQUEST_CONTENT,
                        ReferenceContentId: ((dynamic)originalEvent)?.AppContentId,
                        FailedReason: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED + ": ReQueue Limit Error")
                    );
                }
                else
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: originalEvent as AppContentNormalizedResultEto);
                }

                break;
            }
            case nameof(AnalysisContentNormalizedResultEto):
            {
                if (skipReQueueOperation)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new SetContentStatusToFailedEto(
                        ReferenceContentType: ReferenceContentTypes.ANALYSIS_CONTENT,
                        ReferenceContentId: ((dynamic)originalEvent)?.AnalysisContentId,
                        FailedReason: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED + ": ReQueue Limit Error")
                    );
                }
                else
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: originalEvent as AnalysisContentNormalizedResultEto);
                }

                break;
            }
            case nameof(VideoGenerationResultEto):
            {
                if (skipReQueueOperation)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new SetContentStatusToFailedEto(
                        ReferenceContentType: ((dynamic)originalEvent)?.ReferenceContentType,
                        ReferenceContentId: ((dynamic)originalEvent)?.ReferenceContentId,
                        FailedReason: EventManagerOperationFacilities.EVENT_REQUEUED_FAILED + ": ReQueue Limit Error")
                    );
                }
                else
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: originalEvent as VideoGenerationResultEto);
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