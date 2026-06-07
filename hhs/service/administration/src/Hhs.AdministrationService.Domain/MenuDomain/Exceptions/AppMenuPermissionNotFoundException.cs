using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.MenuDomain.Exceptions;

[Serializable]
internal sealed class AppMenuPermissionNotFoundException : BusinessException
{
    public AppMenuPermissionNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AppMenuPermissionNotFound])
    {
        ErrorCode = DomainErrorCodes.AppMenuPermissionNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}