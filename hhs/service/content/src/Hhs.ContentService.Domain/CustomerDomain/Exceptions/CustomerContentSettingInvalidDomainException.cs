using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.CustomerDomain.Exceptions;

[Serializable]
public class CustomerContentSettingInvalidDomainException : BusinessException
{
    public CustomerContentSettingInvalidDomainException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentSettingInvalidDomain])
    {
        ErrorCode = DomainErrorCodes.CustomerContentSettingInvalidDomain.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}