using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;

/// <summary>There is no such subscription: answered as 404.</summary>
public static class SubscriptionNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Subscription {id} was not found.").WithKey("subscription_not_found", id);
}
