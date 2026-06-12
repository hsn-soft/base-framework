using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.SettingDomain.Exceptions;

[Serializable]
public class CustomerVpSettingInvalidDomainException : BusinessException
{
    public CustomerVpSettingInvalidDomainException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerVpSettingInvalidDomain])
    {
        ErrorCode = DomainErrorCodes.CustomerVpSettingInvalidDomain.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}