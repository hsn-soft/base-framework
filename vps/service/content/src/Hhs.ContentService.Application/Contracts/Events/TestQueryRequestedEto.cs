using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Contracts.Events;

public sealed record TestQueryRequestedEto(Guid CustomerId) : IIntegrationEventMessage
{
    public Guid CustomerId { get; } = CustomerId;
}