using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.Domain.AuthDomain.Exceptions;

[Serializable]
internal sealed class AppUserUsernameDuplicateException : BusinessException
{
    public AppUserUsernameDuplicateException(IStringLocalizer localizer, string username)
        : base(errorMessage: localizer[DomainErrorCodes.AppUserUsernameDuplicate])
    {
        ErrorCode = DomainErrorCodes.AppUserUsernameDuplicate.Split(':').LastOrDefault();
        WithData(localizer[$"{nameof(AppUser)}:{nameof(AppUser.UserName)}"], username);
    }
}