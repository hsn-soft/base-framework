using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.TextNormalizerService.Application.Contracts.Events;

public sealed record AnalysisNormalizedRequestOutlineStartedEto(Guid AnalysisNormalizedRequestId) : IIntegrationEventMessage
{
    public Guid AnalysisNormalizedRequestId { get; } = AnalysisNormalizedRequestId;
}