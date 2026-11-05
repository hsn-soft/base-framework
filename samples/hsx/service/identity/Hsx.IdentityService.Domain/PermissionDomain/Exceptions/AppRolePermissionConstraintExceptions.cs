using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.PermissionDomain.Exceptions;

/// <summary>The grant of a constraint to a role is not there (or the caller may not see it): answered as 404.</summary>
public static class AppRolePermissionConstraintNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"The grant of a constraint to a role {id} was not found.").WithKey("app_role_permission_constraint_not_found", id);
}
