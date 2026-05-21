using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AuthDomain.Exceptions;

[Serializable]
internal sealed class AppUserInvalidTenantDomainException : BusinessException
{
    public AppUserInvalidTenantDomainException(IStringLocalizer localizer, string tenantDomain)
        : base(errorMessage: localizer[DomainErrorCodes.InvalidTenantDomainError])
    {
        ErrorCode = DomainErrorCodes.InvalidTenantDomainError.Split(':').LastOrDefault();
        WithData(localizer["TenantDomain"], tenantDomain ?? string.Empty);
    }
}