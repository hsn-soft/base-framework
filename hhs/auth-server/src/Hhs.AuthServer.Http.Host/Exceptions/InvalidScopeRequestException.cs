using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AuthServer.Exceptions;

[Serializable]
public class InvalidScopeRequestException : BusinessException
{
    public InvalidScopeRequestException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[ApplicationErrorCodes.InvalidScopeRequest])
    {
        ErrorCode = ApplicationErrorCodes.InvalidScopeRequest.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}