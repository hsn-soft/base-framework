using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record CustomerContentNormalizedResultEto(Guid CustomerContentId, bool IsNormalizedSuccess, Guid ContentNormalizedRequestId,DateTime? ReleaseTime) : IIntegrationEventMessage
{
    public Guid CustomerContentId { get; } = CustomerContentId;
    public bool IsNormalizedSuccess { get; } = IsNormalizedSuccess;

    public Guid ContentNormalizedRequestId { get; } = ContentNormalizedRequestId;

    public DateTime? ReleaseTime { get; } = ReleaseTime;
}