using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class PermissionDependencyNotFoundException : BusinessException
{
    public PermissionDependencyNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.PermissionDependencyNotFound])
    {
        ErrorCode = DomainErrorCodes.PermissionDependencyNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}