using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;

/// <summary>A role has a subscription: its users see the data of that product of that customer (the scope key of the subscription). The links are kept by the system from the type of the role (<see cref="AppRoleDomain.Entities.AppRoleType"/>), not edited by hand. The tenant is the role's.</summary>
public sealed class AppRoleSubscription : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AppRoleSubscription()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid SubscriptionId { get; private set; }

    public static AppRoleSubscription Create(Guid roleId, Guid subscriptionId, Guid tenantId) => new() { Id = Guid.CreateVersion7(), RoleId = roleId, SubscriptionId = subscriptionId, TenantId = tenantId };
}
