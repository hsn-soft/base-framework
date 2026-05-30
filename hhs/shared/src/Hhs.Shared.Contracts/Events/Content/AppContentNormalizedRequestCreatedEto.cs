using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record AppContentNormalizedRequestCreatedEto(Guid AppContentId, Guid NormalizedRequestId) : IIntegrationEventMessage
{
    public Guid AppContentId { get; } = AppContentId;
    public Guid NormalizedRequestId { get; } = NormalizedRequestId;
}