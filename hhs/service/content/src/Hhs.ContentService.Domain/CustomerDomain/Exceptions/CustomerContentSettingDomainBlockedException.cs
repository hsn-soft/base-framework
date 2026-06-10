using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.CustomerDomain.Exceptions;

[Serializable]
public class CustomerContentSettingDomainBlockedException : BusinessException
{
    public CustomerContentSettingDomainBlockedException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentSettingDomainBlocked])
    {
        ErrorCode = DomainErrorCodes.CustomerContentSettingDomainBlocked.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}