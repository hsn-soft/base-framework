using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record AnalysisContentNormalizedResultEto(Guid AnalysisContentId, bool IsNormalizedSuccess, Guid NormalizedAnalysisId) : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; } = AnalysisContentId;
    public bool IsNormalizedSuccess { get; } = IsNormalizedSuccess;

    public Guid NormalizedAnalysisId { get; } = NormalizedAnalysisId;
}