using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Exceptions;

[Serializable]
public class InvalidGrantTypeException : BusinessException
{
    public InvalidGrantTypeException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[ApplicationErrorCodes.InvalidGrantType])
    {
        ErrorCode = ApplicationErrorCodes.InvalidGrantType.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}