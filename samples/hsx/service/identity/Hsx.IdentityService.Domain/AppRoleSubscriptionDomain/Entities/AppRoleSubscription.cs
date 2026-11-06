using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;

namespace Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;

/// <summary>A role has a subscription: its users see the data of that product of that customer (the scope key of the subscription). The links are kept by the system from the type of the role (<see cref="Enums.AppRoleType"/>), not edited by hand. The tenant is the role's.</summary>
public sealed class AppRoleSubscription : Entity<Guid>, IMultiTenant
{
    // for EF Core
    private AppRoleSubscription()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid SubscriptionId { get; private set; }

    /// <summary>The users of the role do not see this subscription while it is blocked.</summary>
    public bool IsBlocked { get; private set; }

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded
    public AppRole? Role { get; }
    public Subscription? Subscription { get; }

    public static AppRoleSubscription Create(Guid roleId, Guid subscriptionId, Guid tenantId, Guid? id = null) => new() { Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(), RoleId = roleId, SubscriptionId = subscriptionId, TenantId = tenantId };

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;
}
