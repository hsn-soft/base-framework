using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.TenantDomain.Exceptions;

[Serializable]
public sealed class TenantNotFoundException : BusinessException
{
    public TenantNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.TenantNotFound])
    {
        ErrorCode = DomainErrorCodes.TenantNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}