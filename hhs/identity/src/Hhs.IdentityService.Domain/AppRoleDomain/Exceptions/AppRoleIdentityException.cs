using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Exceptions;

[Serializable]
internal sealed class AppRoleIdentityException : BusinessException
{
    public AppRoleIdentityException(IStringLocalizer localizer, IEnumerable<IdentityError> errors)
        : base(errorMessage: localizer[DomainErrorCodes.AppRoleIdentityError])
    {
        ErrorCode = DomainErrorCodes.AppRoleIdentityError.Split(':').LastOrDefault();
        foreach (var error in errors)
        {
            WithData(localizer[ValidationResourceKeys.ErrorReference] + (string.IsNullOrWhiteSpace(error.Code)
                    ? ""
                    : " " + error.Code),
                error.Description ?? string.Empty);
        }
    }
}