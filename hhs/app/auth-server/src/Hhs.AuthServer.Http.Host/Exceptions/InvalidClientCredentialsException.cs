using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Exceptions;

[Serializable]
public class InvalidClientCredentialsException : BusinessException
{
    public InvalidClientCredentialsException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[ApplicationErrorCodes.InvalidClientCredentials])
    {
        ErrorCode = ApplicationErrorCodes.InvalidClientCredentials.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}