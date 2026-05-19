using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ClientDomain.Exceptions;

[Serializable]
public class ClientInvalidDomainException : BusinessException
{
    public ClientInvalidDomainException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.ClientInvalidDomain])
    {
        ErrorCode = DomainErrorCodes.ClientInvalidDomain.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}