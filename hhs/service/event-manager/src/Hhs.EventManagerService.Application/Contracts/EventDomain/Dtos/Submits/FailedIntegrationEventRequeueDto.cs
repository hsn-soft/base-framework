namespace Hhs.EventManagerService.Application.Contracts.EventDomain.Dtos.Submits;

public sealed class FailedIntegrationEventRequeueDto
{
    public Guid FailedIntegrationEventId { get; set; }
}