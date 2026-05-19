using System.ComponentModel.DataAnnotations;
using Hhs.AuthServer.Application.Localization;
using Hhs.IdentityService.Domain.Localization;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Models;

public sealed class JwtRefreshTokenRequestDto : BaseTokenRequestDto, IValidatableObject
{
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