using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.SubscriptionDomain.Exceptions;

/// <summary>There is no such subscription: answered as 404.</summary>
public static class SubscriptionNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Subscription {id} was not found.").WithKey("subscription_not_found", id);
}

/// <summary>The customer has this product from a reseller already (a subscription that is not ended): two resellers cannot sell the same product to a customer.</summary>
public sealed class SubscriptionAlreadyActiveException : BusinessException
{
    public const string ErrorKey = "subscription_already_active";

    public SubscriptionAlreadyActiveException(Guid customerId, string product)
        : base(ErrorKey, $"The customer {customerId} has the product {product} already.") => WithKey(ErrorKey, customerId, product);
}

/// <summary>The subscription has ended: it is not changed any more, and it is not passed on.</summary>
public sealed class SubscriptionEndedException : BusinessException
{
    public const string ErrorKey = "subscription_ended";

    public SubscriptionEndedException(Guid subscriptionId)
        : base(ErrorKey, $"The subscription {subscriptionId} has ended.") => WithKey(ErrorKey, subscriptionId);
}

/// <summary>A subscription is sold by a reseller: the tenant is not one, or it is the one that sells it already (a transfer is to another).</summary>
public sealed class SubscriptionTenantInvalidException : BusinessException
{
    public const string ErrorKey = "subscription_tenant_invalid";

    public SubscriptionTenantInvalidException(Guid tenantId)
        : base(ErrorKey, $"The tenant {tenantId} cannot be the reseller of this subscription.") => WithKey(ErrorKey, tenantId);
}
