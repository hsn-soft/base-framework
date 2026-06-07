using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Contracts.Events;

public sealed record TrendVideoGenerationQueryEto(Guid AppClientId) : IIntegrationEventMessage
{
    public Guid AppClientId { get; } = AppClientId;
}