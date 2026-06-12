using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.TextNormalizerService.Application.Contracts.Events;

public sealed record NormalizedRequestScrapingStartedEto(Guid NormalizedRequestId) : IIntegrationEventMessage
{
    public Guid NormalizedRequestId { get; } = NormalizedRequestId;
}