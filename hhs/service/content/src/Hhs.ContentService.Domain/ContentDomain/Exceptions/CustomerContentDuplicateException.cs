using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class CustomerContentDuplicateException : BusinessException
{
    public CustomerContentDuplicateException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentDuplicate])
    {
        ErrorCode = DomainErrorCodes.CustomerContentDuplicate.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}