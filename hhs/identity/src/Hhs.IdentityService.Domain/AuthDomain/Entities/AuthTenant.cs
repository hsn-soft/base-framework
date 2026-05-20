using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AuthTenant: AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; set; }

    public Guid? ParentId { get; set; }
    [CanBeNull] public AuthTenant Parent { get; set; }
    public ICollection<AuthTenant> Children { get; set; } = [];

    public string Title { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;

    public string NormalizedAccessPath { get; set; } = null!;

    public bool IsSystemTenant { get; set; }

    public ICollection<AuthUser> Users { get; set; } = [];
    public ICollection<AuthRole> Roles { get; set; } = [];

    public AuthTenant(Guid id) : base(id)
    {

    }
}