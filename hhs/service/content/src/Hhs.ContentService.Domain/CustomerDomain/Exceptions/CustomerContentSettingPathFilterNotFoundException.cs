using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.CustomerDomain.Exceptions;

[Serializable]
public class CustomerContentSettingPathFilterNotFoundException : BusinessException
{
    public CustomerContentSettingPathFilterNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentSettingPathFilterNotFound])
    {
        ErrorCode = DomainErrorCodes.CustomerContentSettingPathFilterNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}