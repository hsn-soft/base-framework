using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AppUserDomain.Exceptions;

[Serializable]
internal sealed class AppUserIdentityException : BusinessException
{
    public AppUserIdentityException(IStringLocalizer localizer, IEnumerable<IdentityError> errors)
        : base(errorMessage: localizer[DomainErrorCodes.AppUserIdentityError])
    {
        ErrorCode = DomainErrorCodes.AppUserIdentityError.Split(':').LastOrDefault();
        foreach (var error in errors)
        {
            WithData(localizer[ValidationResourceKeys.ErrorReference] + (string.IsNullOrWhiteSpace(error.Code)
                    ? ""
                    : " " + error.Code),
                error.Description ?? string.Empty);
        }
    }
}