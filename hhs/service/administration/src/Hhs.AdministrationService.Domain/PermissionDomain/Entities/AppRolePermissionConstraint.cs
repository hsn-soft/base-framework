using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class AppRolePermissionConstraint : Entity<Guid>
{
    public Guid AppRoleId { get; set; }

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; }

    [NotNull] public string Value { get; private set; }

    private AppRolePermissionConstraint()
    {
        // Not-Null string fields
        Value = string.Empty;

        // Navigation fields
        Permission = null;
    }

    internal AppRolePermissionConstraint(Guid appRoleId, Guid permissionId, [NotNull] string value)
        : this(Guid.CreateVersion7(), appRoleId, permissionId, value)
    {
    }

    internal AppRolePermissionConstraint(Guid id, Guid appRoleId, Guid permissionId, [NotNull] string value) : this()
    {
        Id = id;
        AppRoleId = appRoleId;
        PermissionId = permissionId;
        SetValue(value);
    }

    private void SetValue(string value)
    {
        string checkValue = LocalizedModelValidator.NotNullOrWhiteSpace(value, $"{nameof(AppRolePermissionConstraint)}:{nameof(Value)}", AppRolePermissionConstraintConsts.ValueMaxLength);
        Value = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkValue));
    }
}