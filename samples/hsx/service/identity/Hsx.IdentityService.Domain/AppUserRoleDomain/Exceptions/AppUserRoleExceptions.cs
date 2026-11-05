using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AppUserRoleDomain.Exceptions;

/// <summary>The role of a user is not there (or the caller may not see it): answered as 404.</summary>
public static class AppUserRoleNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"The role of a user {id} was not found.").WithKey("app_user_role_not_found", id);
}
