using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Exceptions;

[Serializable]
internal sealed class AppRoleInvalidTenantDomainException : BusinessException
{
    public AppRoleInvalidTenantDomainException(IStringLocalizer localizer, string tenantDomain)
        : base(errorMessage: localizer[DomainErrorCodes.TenantNotFound])
    {
        ErrorCode = DomainErrorCodes.TenantNotFound.Split(':').LastOrDefault();
        WithData(localizer["TenantDomain"], tenantDomain ?? string.Empty);
    }
}