using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ClientDomain.Exceptions;

[Serializable]
public class ClientNotFoundException : BusinessException
{
    public ClientNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.ClientNotFound])
    {
        ErrorCode = DomainErrorCodes.ClientNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}