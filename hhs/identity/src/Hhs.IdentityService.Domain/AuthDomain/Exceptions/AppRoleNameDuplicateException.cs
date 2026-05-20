using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AuthDomain.Exceptions;

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