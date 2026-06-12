using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Filters;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Submits;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;

public interface IFailedIntegrationEventAppService : IEventApplicationService
{
    Task<FailedIntegrationEventDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedDataResultDto<FailedIntegrationEventDto>> GetPagedListAsync(GetFailedIntegrationEventsPaged pagedInput, CancellationToken cancellationToken = default);

    Task CreateAsync(FailedEto input, Guid? failedMessageEnvelopeId);

    Task ReQueueFailedEventByIdAsync(FailedIntegrationEventRequeueDto input);
}