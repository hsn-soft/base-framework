using System.ComponentModel.DataAnnotations;
using Hhs.AdministrationService.Domain.Localization;
using Hhs.AdministrationService.Domain.PermissionDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Submits;

public sealed class PermissionGrantUpdateDto : IValidatableObject
{
    public Guid Id { get; set; }

    [CanBeNull]
    public string Name { get; set; }

    [CanBeNull]
    public string ProviderName { get; set; }

    [CanBeNull]
    public string ProviderKey { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple(new List<Type>
        {
            typeof(AdministrationServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });

        if (Id == Guid.Empty)
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.IsNotEmpty], new[] { "Id" });
        }

        if (!CheckSafe.NotNullOrEmpty(Name, nameof(Name)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["PermissionGrant:Name"].ToString() });
        }
        else if (!CheckSafe.Length(Name, nameof(Name), PermissionGrantConsts.NameMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, PermissionGrantConsts.NameMaxLength], new[] { localizer?["PermissionGrant:Name"].ToString() });
        }

        if (!CheckSafe.NotNullOrEmpty(ProviderName, nameof(ProviderName)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["PermissionGrant:ProviderName"].ToString() });
        }
        else if (!CheckSafe.Length(ProviderName, nameof(ProviderName), PermissionGrantConsts.ProviderNameMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, PermissionGrantConsts.ProviderNameMaxLength], new[] { localizer?["PermissionGrant:ProviderName"].ToString() });
        }

        if (!CheckSafe.NotNullOrEmpty(ProviderKey, nameof(ProviderKey)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["PermissionGrant:ProviderKey"].ToString() });
        }
        else if (!CheckSafe.Length(ProviderKey, nameof(ProviderKey), PermissionGrantConsts.ProviderKeyMaxLength))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.MaxLength, PermissionGrantConsts.ProviderKeyMaxLength], new[] { localizer?["PermissionGrant:ProviderKey"].ToString() });
        }
    }
}