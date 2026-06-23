using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record ContentNormalizedRequestCreatedEto(Guid CustomerContentId, Guid ContentNormalizedRequestId) : IIntegrationEventMessage
{
    public Guid CustomerContentId { get; } = CustomerContentId;
    public Guid ContentNormalizedRequestId { get; } = ContentNormalizedRequestId;
}