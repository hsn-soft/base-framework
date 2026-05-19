using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record AppContentNormalizedResultEto(Guid AppContentId, bool IsNormalizedSuccess, Guid NormalizedRequestId,DateTime? ReleaseTime) : IIntegrationEventMessage
{
    public Guid AppContentId { get; } = AppContentId;
    public bool IsNormalizedSuccess { get; } = IsNormalizedSuccess;

    public Guid NormalizedRequestId { get; } = NormalizedRequestId;

    public DateTime? ReleaseTime { get; } = ReleaseTime;
}