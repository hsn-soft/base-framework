using Hhs.TextNormalizerService.Domain.ContentDomain.Entities;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos;

public sealed class AnalysisNormalizedRequestDto : CreationAuditedEntityDto<Guid>
{
    public Guid TenantId { get; set; }

    public Guid ClientId { get; set; }

    [NotNull] public string DomainName { get; set; } = string.Empty;

    public Guid AnalysisContentId { get; set; }
    public DateTime AnalysisDate { get; set; }
    public AnalysisNormalizedRequestStates OperationStatus { get; set; }
    [CanBeNull] public string OperationStatusDescription { get; set; }

    [NotNull] public List<AnalysisReferenceModel> AnalysisReferenceList { get; set; } = [];

    [CanBeNull] public string CorrelationId { get; set; }
}