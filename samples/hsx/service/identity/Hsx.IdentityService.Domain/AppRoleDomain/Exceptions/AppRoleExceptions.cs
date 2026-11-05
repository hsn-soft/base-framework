using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;

/// <summary>There is no such role (or the caller may not see it): answered as 404.</summary>
public static class AppRoleNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Role {id} was not found.").WithKey("app_role_not_found", id);
}

/// <summary>There is a role with this name in the tenant already.</summary>
public sealed class AppRoleNameDuplicateException : BusinessException
{
    public const string ErrorKey = "app_role_name_duplicate";

    public AppRoleNameDuplicateException(string name)
        : base(ErrorKey, $"A role named '{name}' exists.") => WithKey(ErrorKey, name);
}

/// <summary>A static role is not deleted, and a role that users have is not deleted.</summary>
public sealed class AppRoleNotDeletableException : BusinessException
{
    public const string ErrorKey = "app_role_not_deletable";

    public AppRoleNotDeletableException(string name)
        : base(ErrorKey, $"The role '{name}' is static or has users and cannot be deleted.") => WithKey(ErrorKey, name);
}

/// <summary>The type of a role and the company or customer it names do not go together, or the type does not fit the tenant (the roles of the system tenant are of the type System, the roles of a reseller are not).</summary>
public sealed class AppRoleTypeInvalidException : BusinessException
{
    public const string ErrorKey = "app_role_type_invalid";

    public AppRoleTypeInvalidException(string reason)
        : base(ErrorKey, reason) => WithKey(ErrorKey, reason);
}
