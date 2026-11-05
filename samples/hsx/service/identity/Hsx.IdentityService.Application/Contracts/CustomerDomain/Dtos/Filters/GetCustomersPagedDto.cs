using HsnSoft.Base.Application.Dtos;

namespace Hsx.IdentityService.Application.Contracts.CustomerDomain.Dtos.Filters;

public sealed class GetCustomersPagedDto : PagedDataRequestDto
{
    /// <summary>Only the company id.</summary>
    public Guid? CompanyId { get; set; }
}
