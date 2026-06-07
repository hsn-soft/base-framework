using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.TenantDomain.Exceptions;

[Serializable]
public sealed class UnauthorizedTenantException : BusinessException
{
    public UnauthorizedTenantException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.UnauthorizedTenantError])
    {
        ErrorCode = DomainErrorCodes.UnauthorizedTenantError.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}