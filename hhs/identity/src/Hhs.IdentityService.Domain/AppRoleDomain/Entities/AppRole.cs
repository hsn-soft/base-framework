using System.Globalization;
using System.Text;
using Hhs.IdentityService.Domain.AppRoleDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Entities;

public sealed class AppRole : IdentityRole<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    [NotNull]
    public string TenantDomain { get; private set; }

    public bool IsDefault { get; internal set; }
    public bool IsStatic { get; internal set; }
    public bool IsPublic { get; internal set; }

    private AppRole()
    {
        TenantDomain = string.Empty;
        Name = string.Empty;
    }

    internal AppRole(Guid id, Guid tenantId, string tenantDomain,
        string name,
        bool isDefault = false,
        bool isStatic = false,
        bool isPublic = false
    ) : this()
    {
        Id = id;
        SetTenant(tenantId, tenantDomain);

        SetName(name);
        IsDefault = isDefault;
        IsStatic = isStatic;
        IsPublic = isPublic;
    }

    internal void SetTenant(Guid tenantId, string tenantDomain)
    {
        TenantId = tenantId;
        TenantDomain = LocalizedModelValidator.NotNullOrWhiteSpace(tenantDomain, $"{nameof(TenantDomain)}", AppRoleConsts.TenantDomainMaxLength);
    }

    internal void SetName(string name)
    {
        string checkRoleName = LocalizedModelValidator.NotNullOrWhiteSpace(name, $"{nameof(AppRole)}:{nameof(Name)}", AppRoleConsts.NameMaxLength);

        Name = string.Join("", checkRoleName.ToLower(new CultureInfo("en-US")).Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));

        NormalizedName = string.Join("", Name.ToUpper().Normalize(NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }
}