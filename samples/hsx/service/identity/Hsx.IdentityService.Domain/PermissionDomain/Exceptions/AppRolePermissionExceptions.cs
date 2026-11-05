using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.PermissionDomain.Exceptions;

/// <summary>The grant of a permission to a role is not there (or the caller may not see it): answered as 404.</summary>
public static class AppRolePermissionNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"The grant of a permission to a role {id} was not found.").WithKey("app_role_permission_not_found", id);
}

/// <summary>The caller grants what it does not have itself (a permission it was not granted, or a constraint with a higher value than its own): an administrator cannot give a role more than the administrator has.</summary>
public sealed class AppRolePermissionNotGrantableException : BusinessException
{
    public const string ErrorKey = "app_role_permission_not_grantable";

    public AppRolePermissionNotGrantableException(string permission)
        : base(ErrorKey, $"The permission '{permission}' cannot be granted: the caller does not have it, or not so much of it.") => WithKey(ErrorKey, permission);
}
