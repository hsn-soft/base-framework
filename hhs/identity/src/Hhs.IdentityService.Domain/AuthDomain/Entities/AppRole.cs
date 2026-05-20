using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AuthDomain.Entities;

public sealed class AppRole : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    [CanBeNull] public Tenant Tenant { get; set; }

    [NotNull] public string Name { get; private set; }

    [NotNull] public string NormalizedName { get; private set; }

    public bool IsStatic { get; set; } // can't delete
    public bool IsDefault { get; set; } // register screen user

    public ICollection<AppUserRole> UserRoles { get; set; }
    public ICollection<AppRoleClaim> Claims { get; set; }

    private AppRole()
    {
        // Not-Null string fields
        Name = string.Empty;
        NormalizedName = string.Empty;

        // include arrays
        UserRoles = [];
        Claims = [];
    }

    internal AppRole(Guid tenantId,
        [NotNull] string name,
        bool isDefault = false,
        bool isStatic = false
    ) : this(Guid.CreateVersion7(), tenantId, name, isDefault, isStatic)
    {
    }

    internal AppRole(Guid id, Guid tenantId,
        string name,
        bool isDefault = false,
        bool isStatic = false
    ) : this()
    {
        Id = id;
        TenantId = tenantId;

        SetName(name);
        IsDefault = isDefault;
        IsStatic = isStatic;
    }

    internal void SetName(string name)
    {
        Name = LocalizedModelValidator.NotNullOrWhiteSpace(name, $"{nameof(AppRole)}:{nameof(Name)}", AppRoleConsts.NameMaxLength);
        NormalizedName = StringOperations.Normalize(Name);
    }
}