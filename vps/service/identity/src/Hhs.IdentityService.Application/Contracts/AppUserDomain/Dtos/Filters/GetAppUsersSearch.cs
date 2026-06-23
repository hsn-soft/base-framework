using HsnSoft.Base.Application.Dtos;

namespace Hhs.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;

public sealed class GetAppUsersSearch : SearchDataRequestDto
{
    public Guid? TenantId { get; set; } = null;
}