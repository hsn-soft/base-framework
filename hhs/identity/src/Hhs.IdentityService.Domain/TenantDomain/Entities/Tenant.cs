using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Consts;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.TenantDomain.Entities;

public sealed class Tenant : AuditedEntity<Guid>, ISoftDelete
{
    public bool IsDeleted { get; internal set; }

    public bool IsSystemTenant { get; internal set; }

    public Guid? ParentId { get; set; }
    [CanBeNull] public Tenant Parent { get; set; }
    public ICollection<Tenant> Children { get; set; }

    [NotNull] public string Title { get; private set; }

    [NotNull] public string Name { get; private set; }

    [NotNull] public string NormalizedName { get; private set; }

    [NotNull] public string NormalizedAccessPath { get; private set; }

    public ICollection<AppUser> Users { get; set; }
    public ICollection<AppRole> Roles { get; set; }

    private Tenant()
    {
        // Not-Null string fields
        Title = string.Empty;
        Name = string.Empty;
        NormalizedName = string.Empty;
        NormalizedAccessPath = string.Empty;

        // include arrays
        Children = [];
        Users = [];
        Roles = [];
    }

    internal Tenant(
        [NotNull] string title,
        [NotNull] string name,
        [NotNull] string path,
        bool isSystemTenant = false,
        Guid? parentId = null
    ) : this(Guid.CreateVersion7(), title: title, name: name, path: path, isSystemTenant: isSystemTenant, parentId: parentId)
    {
    }

    internal Tenant(Guid id,
        string title,
        string name,
        string path,
        bool isSystemTenant = false,
        Guid? parentId = null
    ) : this()
    {
        Id = id;
        IsSystemTenant = isSystemTenant;
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
        NormalizedName = StringOperations.Normalize(Name);
    }

    internal void SetPath(string path)
    {
        string checkedValue = LocalizedModelValidator.NotNullOrWhiteSpace(path, $"{nameof(Tenant)}:{nameof(NormalizedAccessPath)}", TenantConsts.NormalizedAccessPathMaxLength);
        NormalizedAccessPath = StringOperations.Normalize(checkedValue);
    }
}