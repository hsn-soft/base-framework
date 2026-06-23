using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record AnalysisNormalizedRequestCreatedEto(Guid AnalysisContentId, Guid AnalysisNormalizedRequestId) : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; } = AnalysisContentId;
    public Guid AnalysisNormalizedRequestId { get; } = AnalysisNormalizedRequestId;
}