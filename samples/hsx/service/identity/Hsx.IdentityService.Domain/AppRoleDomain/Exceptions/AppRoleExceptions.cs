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
    public const string Code = "app_role_name_duplicate";

    public AppRoleNameDuplicateException(string name)
        : base(Code, $"A role named '{name}' exists.") => WithKey(Code, name);
}

/// <summary>A static role is not deleted, and a role that users have is not deleted.</summary>
public sealed class AppRoleNotDeletableException : BusinessException
{
    public const string Code = "app_role_not_deletable";

    public AppRoleNotDeletableException(string name)
        : base(Code, $"The role '{name}' is static or has users and cannot be deleted.") => WithKey(Code, name);
}

/// <summary>A role is given only the subscriptions that were sold by the tenant (or a tenant under it): a subscription of another reseller is not its to give.</summary>
public sealed class SubscriptionNotAllowedException : BusinessException
{
    public const string Code = "subscription_not_allowed";

    public SubscriptionNotAllowedException(Guid subscriptionId)
        : base(Code, $"The subscription {subscriptionId} is not one of the tenant's.") => WithKey(Code, subscriptionId);
}
