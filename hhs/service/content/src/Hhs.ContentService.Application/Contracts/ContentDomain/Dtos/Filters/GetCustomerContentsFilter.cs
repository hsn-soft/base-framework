using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;

public sealed class GetCustomerContentsFilter : SortedAndLimitedDataRequestDto
{
    public Guid? CustomerId { get; set; }
    public ProductTypes? ProductType { get; set; }

    public DateTime? CreationTimeStart { get; set; }
    public DateTime? CreationTimeEnd { get; set; }

    [CanBeNull] public string SlugKey { get; set; }

    public CustomerContentOperationStates? OperationStatus { get; set; }

    public DateTime? ReleaseTimeStart { get; set; }
    public DateTime? ReleaseTimeEnd { get; set; }
}