using Hhs.IdentityService.Domain.TenantDomain.Entities;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Entities;

public sealed class AppRoleSubscription : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; set; }

    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;

    public Guid SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    public bool IsBlocked { get; set; }
}