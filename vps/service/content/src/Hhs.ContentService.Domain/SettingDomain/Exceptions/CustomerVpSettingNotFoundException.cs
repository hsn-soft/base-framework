using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.SettingDomain.Exceptions;

[Serializable]
public class CustomerVpSettingNotFoundException : BusinessException
{
    public CustomerVpSettingNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerVpSettingNotFound])
    {
        ErrorCode = DomainErrorCodes.CustomerVpSettingNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}