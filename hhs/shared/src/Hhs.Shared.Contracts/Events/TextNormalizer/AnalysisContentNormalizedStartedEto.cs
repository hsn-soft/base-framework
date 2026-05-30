using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.TextNormalizer;

public sealed record AnalysisContentNormalizedStartedEto(Guid TenantId, Guid ClientId, [NotNull] string DomainName, Guid AnalysisContentId, [NotNull] List<Guid> AppContentIdList, DateTime AnalysisDate) : IIntegrationEventMessage
{
    public Guid TenantId { get; } = TenantId;

    public Guid ClientId { get; } = ClientId;

    [NotNull]
    public string DomainName { get; } = DomainName;

    public Guid AnalysisContentId { get; } = AnalysisContentId;

    [NotNull]
    public List<Guid> AppContentIdList { get; } = AppContentIdList;

    public DateTime AnalysisDate { get; } = AnalysisDate;
}