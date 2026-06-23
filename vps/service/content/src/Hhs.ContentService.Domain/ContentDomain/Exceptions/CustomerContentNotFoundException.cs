using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class CustomerContentNotFoundException : BusinessException
{
    public CustomerContentNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentNotFound])
    {
        ErrorCode = DomainErrorCodes.CustomerContentNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}