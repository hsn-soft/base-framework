using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ClientDomain.Exceptions;

[Serializable]
public class ClientDomainBlockedException : BusinessException
{
    public ClientDomainBlockedException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.ClientDomainBlocked])
    {
        ErrorCode = DomainErrorCodes.ClientDomainBlocked.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}