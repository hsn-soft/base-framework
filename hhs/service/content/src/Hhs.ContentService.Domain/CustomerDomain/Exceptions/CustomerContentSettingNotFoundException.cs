using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.CustomerDomain.Exceptions;

[Serializable]
public class CustomerContentSettingNotFoundException : BusinessException
{
    public CustomerContentSettingNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentSettingNotFound])
    {
        ErrorCode = DomainErrorCodes.CustomerContentSettingNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}