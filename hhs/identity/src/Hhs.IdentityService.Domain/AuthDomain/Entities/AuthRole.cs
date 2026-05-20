using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthRole: AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get;  set; }
    public AuthTenant Tenant { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;

    public bool IsStatic { get; set; }  // can't delete
    public bool IsDefault { get; set; }  // register screen user

    public ICollection<AuthUserRole> UserRoles { get; set; } = [];
    public ICollection<AuthRoleClaim> Claims { get; set; } = [];
}