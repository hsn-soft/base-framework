using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Exceptions;

/// <summary>The link of a role to a subscription is not there (or the caller may not see it): answered as 404.</summary>
public static class AppRoleSubscriptionNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"The link of a role to a subscription {id} was not found.").WithKey("app_role_subscription_not_found", id);
}
