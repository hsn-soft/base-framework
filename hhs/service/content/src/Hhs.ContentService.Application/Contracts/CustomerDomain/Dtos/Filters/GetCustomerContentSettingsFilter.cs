using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.ContentService.Application.Contracts.CustomerDomain.Dtos.Filters;

public sealed class GetCustomerContentSettingsFilter : SortedAndLimitedDataRequestDto
{
    public Guid? TenantId { get; set; }

    [CanBeNull] public string DomainName { get; set; }

    public bool? IsBlocked { get; set; }
}