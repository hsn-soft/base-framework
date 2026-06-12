using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.TextNormalizer;

public sealed record AnalysisContentNormalizedStartedEto([NotNull] string ScopeKey, [NotNull] string DomainName, Guid AnalysisContentId, [NotNull] List<Guid> AppContentIdList, DateTime AnalysisDate) : IIntegrationEventMessage
{
    [NotNull] public string ScopeKey { get; } = ScopeKey;

    [NotNull] public string DomainName { get; } = DomainName;

    public Guid AnalysisContentId { get; } = AnalysisContentId;

    [NotNull] public List<Guid> AppContentIdList { get; } = AppContentIdList;

    public DateTime AnalysisDate { get; } = AnalysisDate;
}