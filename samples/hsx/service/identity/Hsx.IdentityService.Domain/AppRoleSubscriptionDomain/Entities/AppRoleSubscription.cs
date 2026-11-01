using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;

/// <summary>A role has a subscription: its users see the data of that product of that customer (the scope key of the subscription). The tenant is kept here and given by the persistence.</summary>
public sealed class AppRoleSubscription : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AppRoleSubscription()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public static AppRoleSubscription Create(Guid roleId, Guid subscriptionId) => new() { Id = Guid.CreateVersion7(), RoleId = roleId, SubscriptionId = subscriptionId };
}
