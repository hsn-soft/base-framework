using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.MenuDomain.Exceptions;

[Serializable]
internal sealed class AppMenuNotFoundException : BusinessException
{
    public AppMenuNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AppMenuNotFound])
    {
        ErrorCode = DomainErrorCodes.AppMenuNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}