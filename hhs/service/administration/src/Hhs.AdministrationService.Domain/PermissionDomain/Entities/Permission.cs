using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class Permission : Entity<Guid>
{
    [NotNull] public string UniqueCode { get; private set; }

    public PermissionTypes PermissionType { get; set; }

    [CanBeNull] public string Name { get; private set; }

    [CanBeNull] public string Description { get; private set; }

    // Navigation fields
    public ICollection<PermissionDependency> ParentPermissions { get; set; }
    public ICollection<PermissionDependency> ChildPermissions { get; set; }

    private Permission()
    {
        // Not-Null string fields
        UniqueCode = string.Empty;

        // Navigation fields
        ParentPermissions = [];
        ChildPermissions = [];
    }

    internal Permission(
        [NotNull] string uniqueCode,
        PermissionTypes permissionType,
        [CanBeNull] string name = null,
        [CanBeNull] string description = null
    ) : this(Guid.CreateVersion7(), uniqueCode, permissionType, name, description)
    {
    }

    internal Permission(Guid id,
        [NotNull] string uniqueCode,
        PermissionTypes permissionType,
        [CanBeNull] string name = null,
        [CanBeNull] string description = null
    ) : this()
    {
        Id = id;

        SetUniqueCode(uniqueCode);
        PermissionType = permissionType;
        SetName(name);
        SetDescription(description);
    }

    private void SetUniqueCode(string uniqueCode)
    {
        string checkValue = LocalizedModelValidator.NotNullOrWhiteSpace(uniqueCode, $"{nameof(Permission)}:{nameof(UniqueCode)}", PermissionConsts.UniqueCodeMaxLength);
        UniqueCode = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkValue));
    }

    internal void SetName(string name) =>
        Name = !string.IsNullOrWhiteSpace(name)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(name, $"{nameof(Permission)}:{nameof(Name)}", PermissionConsts.NameMaxLength)
            : null;

    internal void SetDescription(string description) =>
        Description = !string.IsNullOrWhiteSpace(description)
            ? LocalizedModelValidator.NotNullOrWhiteSpace(description, $"{nameof(Permission)}:{nameof(Description)}", PermissionConsts.DescriptionMaxLength)
            : null;
}