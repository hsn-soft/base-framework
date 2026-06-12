using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.SettingDomain.Exceptions;

[Serializable]
public class CustomerVpSettingDomainBlockedException : BusinessException
{
    public CustomerVpSettingDomainBlockedException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerVpSettingDomainBlocked])
    {
        ErrorCode = DomainErrorCodes.CustomerVpSettingDomainBlocked.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}