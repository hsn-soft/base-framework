using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Exceptions;

[Serializable]
internal sealed class PermissionGrantDuplicateException : BusinessException
{
    public PermissionGrantDuplicateException(IStringLocalizer localizer, string name, string providerName, string providerKey)
        : base(errorMessage: localizer[DomainErrorCodes.PermissionGrantDuplicate])
    {
        ErrorCode = DomainErrorCodes.PermissionGrantDuplicate.Split(':').LastOrDefault();
        WithData(localizer[$"{nameof(PermissionGrant)}:{nameof(PermissionGrant.Name)}"], name);
        WithData(localizer[$"{nameof(PermissionGrant)}:{nameof(PermissionGrant.ProviderName)}"], providerName);
        WithData(localizer[$"{nameof(PermissionGrant)}:{nameof(PermissionGrant.ProviderKey)}"], providerKey);
    }
}