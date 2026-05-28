using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Application.Exceptions;

[Serializable]
public class UserDisabledException : BusinessException
{
    public UserDisabledException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[ApplicationErrorCodes.UserDisabledError])
    {
        ErrorCode = ApplicationErrorCodes.UserDisabledError.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}