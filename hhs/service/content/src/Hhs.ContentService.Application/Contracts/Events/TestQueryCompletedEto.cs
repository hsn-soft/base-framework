using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Contracts.Events;

public sealed record TestQueryCompletedEto(Guid CustomerId) : IIntegrationEventMessage
{
    public Guid CustomerId { get; } = CustomerId;
}