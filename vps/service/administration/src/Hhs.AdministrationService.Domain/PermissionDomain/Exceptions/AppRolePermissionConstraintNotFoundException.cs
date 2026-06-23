using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class AppRolePermissionConstraintNotFoundException : BusinessException
{
    public AppRolePermissionConstraintNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AppRolePermissionConstraintNotFound])
    {
        ErrorCode = DomainErrorCodes.AppRolePermissionConstraintNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}