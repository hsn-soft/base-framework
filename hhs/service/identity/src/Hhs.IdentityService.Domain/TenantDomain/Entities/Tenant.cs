using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.Enums;
using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class Tenant : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public Guid? ParentId { get; set; }
    [CanBeNull] public Tenant Parent { get; set; }
    public ICollection<Tenant> Children { get; set; }

    public TenantTypes TenantType { get; internal set; }

    [NotNull] public string Title { get; private set; }

    [NotNull] public string Name { get; private set; }

    [NotNull] public string NormalizedName { get; private set; }

    [NotNull] public string NormalizedAccessPath { get; private set; }

    public ICollection<AppUser> Users { get; set; }
    public ICollection<AppRole> Roles { get; set; }

    private Tenant()
    {
        // Not-Null string fields
        TenantType = TenantTypes.Unknown;
        Title = string.Empty;
        Name = string.Empty;
        NormalizedName = string.Empty;
        NormalizedAccessPath = string.Empty;

        // navigation fields
        Parent = null;
        Children = [];
        Users = [];
        Roles = [];
    }

    internal Tenant(
        TenantTypes tenantType,
        [NotNull] string title,
        [NotNull] string name,
        [NotNull] string path,
        Guid? parentId = null
    ) : this(Guid.CreateVersion7(), tenantType: tenantType, title: title, name: name, path: path, parentId: parentId)
    {
    }

    internal Tenant(Guid id,
        TenantTypes tenantType,
        [NotNull] string title,
        [NotNull] string name,
        [NotNull] string path,
        Guid? parentId = null
    ) : this()
    {
        Id = id;
        TenantType = tenantType;
        ParentId = parentId;

        SetTitle(title);
        SetName(name);
        SetPath(path);
    }

    internal void SetTitle(string title)
        => Title = LocalizedModelValidator.NotNullOrWhiteSpace(title, $"{nameof(Tenant)}:{nameof(Title)}", TenantConsts.TitleMaxLength);

    internal void SetName(string name)
    {
        Name = LocalizedModelValidator.NotNullOrWhiteSpace(name, $"{nameof(Tenant)}:{nameof(Name)}", TenantConsts.NameMaxLength);
        NormalizedName = StringHelper.Normalize(StringHelper.ReplaceInvalidChars(name));
    }

    internal void SetPath(string path)
    {
        string checkedValue = LocalizedModelValidator.NotNullOrWhiteSpace(path, $"{nameof(Tenant)}:{nameof(NormalizedAccessPath)}", TenantConsts.NormalizedAccessPathMaxLength);
        NormalizedAccessPath = StringHelper.Normalize(checkedValue);
    }
}