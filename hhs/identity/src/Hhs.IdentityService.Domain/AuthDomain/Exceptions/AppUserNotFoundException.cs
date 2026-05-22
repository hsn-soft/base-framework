using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AuthDomain.Exceptions;

[Serializable]
public sealed class AppUserNotFoundException : BusinessException
{
    public AppUserNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AppUserNotFound])
    {
        ErrorCode = DomainErrorCodes.AppUserNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}