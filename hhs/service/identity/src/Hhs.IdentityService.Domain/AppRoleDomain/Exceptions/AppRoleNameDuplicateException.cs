using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Exceptions;

[Serializable]
internal sealed class AppRoleNameDuplicateException : BusinessException
{
    public AppRoleNameDuplicateException(IStringLocalizer localizer, string name)
        : base(errorMessage: localizer[DomainErrorCodes.AppRoleNameDuplicate])
    {
        ErrorCode = DomainErrorCodes.AppRoleNameDuplicate.Split(':').LastOrDefault();
        WithData(localizer[$"{nameof(AppRole)}:{nameof(AppRole.Name)}"], name);
    }
}