using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;

public sealed class GetRolePermissionsFilter
{
    [CanBeNull]
    public string RoleUniqueName { get; set; } = null;
}
