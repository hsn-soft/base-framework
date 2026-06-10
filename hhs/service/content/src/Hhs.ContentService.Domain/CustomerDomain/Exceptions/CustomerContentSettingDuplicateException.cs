using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.CustomerDomain.Exceptions;

[Serializable]
public class CustomerContentSettingDuplicateException : BusinessException
{
    public CustomerContentSettingDuplicateException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentSettingDuplicate])
    {
        ErrorCode = DomainErrorCodes.CustomerContentSettingDuplicate.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}