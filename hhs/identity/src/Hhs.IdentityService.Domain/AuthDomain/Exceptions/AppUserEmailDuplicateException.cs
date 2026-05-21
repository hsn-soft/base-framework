using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AuthDomain.Exceptions;

[Serializable]
internal sealed class AppUserEmailDuplicateException : BusinessException
{
    public AppUserEmailDuplicateException(IStringLocalizer localizer, string email)
        : base(errorMessage: localizer[DomainErrorCodes.AppUserEmailDuplicate])
    {
        ErrorCode = DomainErrorCodes.AppUserEmailDuplicate.Split(':').LastOrDefault();
        WithData(localizer[$"{nameof(AppUser)}:{nameof(AppUser.Email)}"], email);
    }
}