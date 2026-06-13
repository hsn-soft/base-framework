using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.TextNormalizerService.Application.Contracts.Events;

public sealed record ContentNormalizedRequestOutlineStartedEto(Guid ContentNormalizedRequestId) : IIntegrationEventMessage
{
    public Guid ContentNormalizedRequestId { get; } = ContentNormalizedRequestId;
}