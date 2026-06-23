using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;

public sealed class GetAppRolesFilter : SortedAndLimitedDataRequestDto
{
    public Guid? TenantId { get; set; } = null;

    [CanBeNull]
    public string Name { get; set; } = null;

    public bool? IsDefault { get; set; } = null;
    public bool? IsStatic { get; set; } = null;
}