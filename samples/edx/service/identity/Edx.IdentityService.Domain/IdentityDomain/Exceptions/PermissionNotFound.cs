using HsnSoft.Base;

namespace Edx.IdentityService.Domain.IdentityDomain.Exceptions;

/// <summary>There is no such permission, or it is not active any more: answered as 404.</summary>
public static class PermissionNotFound
{
    public static NotFoundException For(string code) => new NotFoundException($"Permission '{code}' was not found.").WithKey("permission_not_found", code);
}

/// <summary>There is no such role (or it is another tenant's): answered as 404.</summary>
public static class RoleNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Role {id} was not found.").WithKey("role_not_found", id);
}
