using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.SettingDomain.Exceptions;

[Serializable]
public class CustomerVpSettingDuplicateException : BusinessException
{
    public CustomerVpSettingDuplicateException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerVpSettingDuplicate])
    {
        ErrorCode = DomainErrorCodes.CustomerVpSettingDuplicate.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}