using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class CustomerContentStateException : BusinessException
{
    public CustomerContentStateException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentStateError])
    {
        ErrorCode = DomainErrorCodes.CustomerContentStateError.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}