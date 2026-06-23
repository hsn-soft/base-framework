using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;

public sealed class GetAnalysisNormalizedRequestPaged : PagedDataRequestDto
{
    public Guid? CustomerId { get; set; }
    public ProductTypes? ProductType { get; set; }
    public Guid? AnalysisContentId { get; set; }

    public DateTime? CreationTimeStart { get; set; }
    public DateTime? CreationTimeEnd { get; set; }

    [CanBeNull] public string DomainName { get; set; }

    public AnalysisNormalizedRequestStates? OperationStatus { get; set; }

    public DateTime? AnalysisDateStart { get; set; }
    public DateTime? AnalysisDateEnd { get; set; }
}