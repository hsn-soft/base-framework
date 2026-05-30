using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.Content;

public sealed record AnalysisContentNormalizedAnalysisCreatedEto(Guid AnalysisContentId, Guid NormalizedAnalysisId) : IIntegrationEventMessage
{
    public Guid AnalysisContentId { get; } = AnalysisContentId;
    public Guid NormalizedAnalysisId { get; } = NormalizedAnalysisId;
}