using HsnSoft.Base.Application.Dtos;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;

public sealed class GetAppRolesSearch : SearchDataRequestDto
{
    public Guid? TenantId { get; set; } = null;
}