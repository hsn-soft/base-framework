using System.ComponentModel.DataAnnotations;
using Hhs.IdentityService.Domain.AppRoleDomain.Consts;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;

public sealed class AppRoleCreateDto : IValidatableObject
{
    public Guid? TenantId { get; set; }

    [CanBeNull]
    public string TenantDomain { get; set; }

    [CanBeNull]
    public string Name { get; set; }

    public bool IsDefault { get; set; }
    public bool IsStatic { get; set; }
    public bool IsPublic { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple(new List<Type>
        {
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });

        if (!CheckSafe.NotNull(TenantId, nameof(TenantId)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.IsNotEmpty], new[] { localizer?["TenantId"].ToString() });
        }

        if (!CheckSafe.NotNullOrEmpty(TenantDomain, nameof(TenantDomain)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["TenantDomain"].ToString() });
        }
        else if (!CheckSafe.Length(TenantDomain, nameof(TenantDomain), AppRoleConsts.TenantDomainMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppRoleConsts.TenantDomainMaxLength], new[] { localizer?["TenantDomain"].ToString() });
        }

        if (!CheckSafe.NotNullOrEmpty(Name, nameof(Name)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["AppRole:Name"].ToString() });
        }
        else if (!CheckSafe.Length(Name, nameof(Name), AppRoleConsts.NameMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, AppRoleConsts.NameMaxLength], new[] { localizer?["AppRole:Name"].ToString() });
        }
    }
}