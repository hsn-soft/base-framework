using System.ComponentModel.DataAnnotations;
using Hhs.AuthServer.Application.Localization;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Serilog.Mask;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Models;

public sealed class JwtPasswordTokenRequestDto : BaseTokenRequestDto, IValidatableObject
{
    [CanBeNull]
    public string UserName { get; set; }

    [SensitiveData]
    [CanBeNull]
    public string Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var factory = validationContext.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        var localizer = factory?.CreateMultiple(new List<Type>
        {
            typeof(AuthServerResource),
            typeof(IdentityServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        });

        if (!CheckSafe.NotNullOrWhiteSpace(UserName, nameof(UserName)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["UserName"].ToString() });
        }

        if (!CheckSafe.NotNullOrWhiteSpace(Password, nameof(Password)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["Password"].ToString() });
        }

        if (!CheckSafe.NotNullOrWhiteSpace(ClientId, nameof(ClientId)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["ClientId"].ToString() });
        }

        if (!CheckSafe.NotNullOrWhiteSpace(GrantType, nameof(GrantType)))
        {
            yield return new ValidationResult(localizer?[ValidationResourceKeys.Required], new[] { localizer?["GrantType"].ToString() });
        }
    }
}