using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Exceptions;

[Serializable]
public class InvalidUserCredentialsException : BusinessException
{
    public InvalidUserCredentialsException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[ApplicationErrorCodes.InvalidUserCredentials])
    {
        ErrorCode = ApplicationErrorCodes.InvalidUserCredentials.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}