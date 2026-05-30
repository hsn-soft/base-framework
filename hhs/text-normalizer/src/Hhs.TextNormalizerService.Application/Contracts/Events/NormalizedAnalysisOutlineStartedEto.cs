using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.TextNormalizerService.Application.Contracts.Events;

public sealed record NormalizedAnalysisOutlineStartedEto(Guid NormalizedAnalysisId) : IIntegrationEventMessage
{
    public Guid NormalizedAnalysisId { get; } = NormalizedAnalysisId;
}