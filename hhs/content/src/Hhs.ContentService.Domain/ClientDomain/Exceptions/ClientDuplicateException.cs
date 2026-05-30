using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ClientDomain.Exceptions;

[Serializable]
public class ClientDuplicateException : BusinessException
{
    public ClientDuplicateException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.ClientDuplicate])
    {
        ErrorCode = DomainErrorCodes.ClientDuplicate.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}