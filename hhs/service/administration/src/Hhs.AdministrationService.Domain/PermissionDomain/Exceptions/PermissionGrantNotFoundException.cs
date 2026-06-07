using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class PermissionGrantNotFoundException : BusinessException
{
    public PermissionGrantNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.PermissionGrantNotFound])
    {
        ErrorCode = DomainErrorCodes.PermissionGrantNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}