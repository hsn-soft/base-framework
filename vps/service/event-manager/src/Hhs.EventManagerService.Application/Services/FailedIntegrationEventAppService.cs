using System.Net;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Filters;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Submits;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;
using Hhs.EventManagerService.Domain.Enums;
using Hhs.EventManagerService.Domain.EventDomain.Consts;
using Hhs.EventManagerService.Domain.EventDomain.Entities;
using Hhs.EventManagerService.Domain.EventDomain.Repositories;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ExpandoObject = System.Dynamic.ExpandoObject;

namespace Hhs.EventManagerService.Application.Services;

public sealed class FailedIntegrationEventAppService : ApplicationServiceBase, IFailedIntegrationEventAppService
{
    private readonly IFrameworkLogger _logger;
    private readonly IFailedIntegrationEventRepository _failedIntegrationEventRepository;

    public FailedIntegrationEventAppService(IServiceProvider provider,
        IFailedIntegrationEventRepository failedIntegrationEventRepository
    ) : base(provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _failedIntegrationEventRepository = failedIntegrationEventRepository;
    }

    public async Task<FailedIntegrationEventDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var item = await _failedIntegrationEventRepository.GetByIdOrDefaultAsync(id, cancellationToken: cancellationToken);
        if (item == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        return Mapper.Map<FailedIntegrationEvent, FailedIntegrationEventDto>(item);
    }

    public async Task<PagedDataResultDto<FailedIntegrationEventDto>> GetPagedListAsync(GetFailedIntegrationEventsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        if (pagedInput == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        // pagedInput.Name = pagedInput.Name?.ToLower(new CultureInfo("en-US"));
        // pagedInput.ProviderName = pagedInput.ProviderName?.ToLower(new CultureInfo("en-US"));
        // pagedInput.ProviderKey = pagedInput.ProviderKey?.ToLower(new CultureInfo("en-US"));
        //
        // var filter = new FilterBuilder<FailedIntegrationEvent>()
        //     .And(!string.IsNullOrWhiteSpace(pagedInput.Name) ? e => e.Name.ToLower(new CultureInfo("en-US")).Contains(pagedInput.Name) : null)
        //     .And(!string.IsNullOrWhiteSpace(pagedInput.ProviderName) ? e => e.ProviderName.ToLower(new CultureInfo("en-US")).Contains(pagedInput.ProviderName) : null)
        //     .And(!string.IsNullOrWhiteSpace(pagedInput.ProviderKey) ? e => e.ProviderKey.ToLower(new CultureInfo("en-US")).Contains(pagedInput.ProviderKey) : null)
        //     .Build();

        var result = await _failedIntegrationEventRepository.GetPageListAsync<FailedIntegrationEventDto>(
            options: new PagedQueryOptions<FailedIntegrationEvent>
            {
                // Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(pagedInput.SortingText)
                    ? FailedIntegrationEventConsts.GetDefaultSorting()
                    : pagedInput.SortingText,
                PageNumber = pagedInput.PageNumber,
                MaxResultCount = pagedInput.MaxResultCount
            }, Mapper.ConfigurationProvider, cancellationToken: cancellationToken);

        return new PagedDataResultDto<FailedIntegrationEventDto>(result.TotalCount, pagedInput.PageNumber, pagedInput.MaxResultCount, result.Items);
    }

    public async Task CreateAsync(FailedEto input, Guid? failedMessageEnvelopeId)
    {
        if (input == null || ParentIntegrationEvent == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        object failedMessageObject = null;
        string serializedFailedMessageObject = input.FailedMessageObject?.ToString();
        if (!string.IsNullOrWhiteSpace(serializedFailedMessageObject))
        {
            failedMessageObject = JsonConvert.DeserializeObject<ExpandoObject>(serializedFailedMessageObject);
            // test = JsonConvert.DeserializeObject<Tester2>(doc.Response.ToString()) as Tester2;
        }

        try
        {
            await _failedIntegrationEventRepository.CreateAsync(
                id: ParentIntegrationEvent?.MessageId ?? Guid.NewGuid(),
                envelopeTime: ParentIntegrationEvent?.MessageTime.ToUniversalTime() ?? DateTime.UtcNow,
                failedReason: input?.FailedReason ?? string.Empty,
                operationStatus: FailedIntegrationEventStates.CreatedWaitForHandling,
                operationStatusDescription: FailedIntegrationEventOperationFacilities.ERROR_HANDLING_CREATED,
                correlationId: ParentIntegrationEvent?.CorrelationId,
                producer: ParentIntegrationEvent?.Producer,
                channel: ParentIntegrationEvent?.ClientChannel,
                userId: ParentIntegrationEvent?.UserId,
                userRoleUniqueName: ParentIntegrationEvent?.UserRoles,
                failedMessageEnvelopeId: failedMessageEnvelopeId,
                failedMessageEnvelopeTime: input?.FailedMessageEnvelopeTime,
                failedMessageObject: failedMessageObject,
                failedMessageTypeName: input.FailedMessageTypeName,
                hopLevel: (ushort)ParentIntegrationEvent?.HopLevel,
                reQueuedCount: (ushort)ParentIntegrationEvent.ReQueuedCount
            );

            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Failed integration event created",
                reference: new { Consumer = ParentIntegrationEvent.Producer, ConsumerMessageType = input.FailedMessageTypeName, input.FailedReason },
                facility: FailedIntegrationEventOperationFacilities.ERROR_HANDLING_CREATED,
                correlationId: ParentIntegrationEvent?.CorrelationId,
                exception: null
            ));
        }
        catch (Exception e)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Failed integration event error: {e.Message}",
                reference: new { Consumer = ParentIntegrationEvent.Producer, ConsumerMessageType = input.FailedMessageTypeName, input.FailedReason },
                facility: FailedIntegrationEventOperationFacilities.ERROR_HANDLING_FAILED,
                correlationId: ParentIntegrationEvent?.CorrelationId,
                exception: null
            ));
        }
    }

    public async Task ReQueueFailedEventByIdAsync(FailedIntegrationEventRequeueDto input)
    {
        if (input?.FailedIntegrationEventId == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var failedIntegrationEventId = (Guid)(input?.FailedIntegrationEventId);

        var failedIntegrationEventItem = await _failedIntegrationEventRepository.GetSingleOrDefaultAsync(x =>
            x.Id == failedIntegrationEventId
            && x.OperationStatus == FailedIntegrationEventStates.CreatedWaitForHandling
        );

        if (failedIntegrationEventItem == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.NotFound);
        }

        bool reQueuedOperationSuccess;
        string errorMessage = null;
        try
        {
            var parent = new ParentMessageEnvelope
            {
                HopLevel = failedIntegrationEventItem.HopLevel,
                ReQueuedCount = failedIntegrationEventItem.ReQueuedCount,
                MessageId = failedIntegrationEventItem.FailedMessageEnvelopeId ?? Guid.NewGuid(),
                MessageTime = failedIntegrationEventItem.FailedMessageEnvelopeTime ?? DateTime.UtcNow,
                CorrelationId = failedIntegrationEventItem.CorrelationId,
                UserId = failedIntegrationEventItem.UserId,
                UserRoles = failedIntegrationEventItem.UserRoleUniqueName,
                ClientChannel = failedIntegrationEventItem.Channel,
                Producer = failedIntegrationEventItem.Producer
            };

            // // Integration Event for ReQueued
            await EventBus.PublishAsync(parentMessage: parent,
                eventMessage: new ReQueuedEto(failedIntegrationEventItem.Producer, failedIntegrationEventItem.FailedMessageObject, failedIntegrationEventItem.FailedMessageTypeName),
                isExchangeEvent: false,
                isReQueuePublish: true
            );

            reQueuedOperationSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.FrameworkErrorLog(LogHelper.Generate(
                message: $"Failed integration event re-queued error: {ex.Message}",
                reference: new { Consumer = failedIntegrationEventItem.Producer, ConsumerMessageType = failedIntegrationEventItem.FailedMessageTypeName, failedIntegrationEventItem.FailedReason },
                facility: FailedIntegrationEventOperationFacilities.ERROR_HANDLING_FAILED,
                correlationId: failedIntegrationEventItem?.CorrelationId,
                exception: null
            ));

            reQueuedOperationSuccess = false;
            errorMessage = ex.Message;
        }

        var updated = await _failedIntegrationEventRepository.ReQueuedResultAsync(
            id: failedIntegrationEventId,
            isReQueuedSuccess: reQueuedOperationSuccess,
            errorMessage: errorMessage);

        if (updated.OperationStatus == FailedIntegrationEventStates.OperationSuccess)
        {
            _logger.FrameworkInfoLog(LogHelper.Generate(
                message: $"Failed integration event successfully re-queued",
                reference: new { Consumer = failedIntegrationEventItem.Producer, ConsumerMessageType = failedIntegrationEventItem.FailedMessageTypeName, failedIntegrationEventItem.FailedReason },
                facility: FailedIntegrationEventOperationFacilities.ERROR_HANDLING_SUCCESS,
                correlationId: failedIntegrationEventItem?.CorrelationId,
                exception: null
            ));
        }
    }
}