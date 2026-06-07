using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class PermissionNotFoundException : BusinessException
{
    public PermissionNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.PermissionNotFound])
    {
        ErrorCode = DomainErrorCodes.PermissionNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}