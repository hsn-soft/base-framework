using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.FeedRService.Application.Contracts.Events;

public sealed record TestQueryRequestedEto(Guid AppClientId) : IIntegrationEventMessage
{
    public Guid AppClientId { get; } = AppClientId;
}