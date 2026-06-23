using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Filters;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Submits;
using Hhs.EventManagerService.Application.Contracts.EventDomain.Interfaces;
using Hhs.EventManagerService.Controllers.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.EventManagerService.Controllers;

[Route("api/event-manager-service/v1/commercial/events")]
public sealed class EventController : BaseServiceController
{
    private readonly IFailedIntegrationEventAppService _failedIntegrationEventAppService;

    public EventController(IServiceProvider provider, IFailedIntegrationEventAppService failedIntegrationEventAppService) : base(provider)
    {
        _failedIntegrationEventAppService = failedIntegrationEventAppService;
    }

    [HttpGet("failed-integration-event/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<FailedIntegrationEventDto> GetFailedIntegrationEventAsync(Guid id) => await _failedIntegrationEventAppService.GetAsync(id);

    [HttpPost("failed-integration-event/paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<FailedIntegrationEventDto>> GetFailedIntegrationEventPagedListAsync([FromBody] GetFailedIntegrationEventsPaged pagedInput) => await _failedIntegrationEventAppService.GetPagedListAsync(pagedInput);

    [HttpPost("failed-integration-event/re-queued-event-by-id")]
    public async Task ReQueueFailedEventByIdAsync(FailedIntegrationEventRequeueDto input) => await _failedIntegrationEventAppService.ReQueueFailedEventByIdAsync(input);
}