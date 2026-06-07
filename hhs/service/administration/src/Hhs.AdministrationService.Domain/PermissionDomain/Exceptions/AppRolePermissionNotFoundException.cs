using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class AppRolePermissionNotFoundException : BusinessException
{
    public AppRolePermissionNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AppRolePermissionNotFound])
    {
        ErrorCode = DomainErrorCodes.AppRolePermissionNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}