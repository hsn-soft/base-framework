using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Application.Contracts.ContentDomain.Dtos.Filters;

public sealed class GetContentNormalizedRequestsPaged : PagedDataRequestDto
{
    public Guid? CustomerId { get; set; }
    public ProductTypes? ProductType { get; set; }
    public Guid? CustomerContentId { get; set; }

    public DateTime? CreationTimeStart { get; set; }
    public DateTime? CreationTimeEnd { get; set; }

    [CanBeNull] public string DomainName { get; set; }
    [CanBeNull] public string DomainPath { get; set; }

    public ContentNormalizedRequestStates? OperationStatus { get; set; }

    public DateTime? ReleaseTimeStart { get; set; }
    public DateTime? ReleaseTimeEnd { get; set; }
}