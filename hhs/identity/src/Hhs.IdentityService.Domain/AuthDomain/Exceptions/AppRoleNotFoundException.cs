using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AuthDomain.Exceptions;

[Serializable]
public sealed class AppRoleNotFoundException : BusinessException
{
    public AppRoleNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AppRoleNotFound])
    {
        ErrorCode = DomainErrorCodes.AppRoleNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}