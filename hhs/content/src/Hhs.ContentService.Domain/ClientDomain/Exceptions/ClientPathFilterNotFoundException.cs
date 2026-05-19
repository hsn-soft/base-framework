using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ClientDomain.Exceptions;

[Serializable]
public class ClientPathFilterNotFoundException : BusinessException
{
    public ClientPathFilterNotFoundException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.ClientPathFilterNotFound])
    {
        ErrorCode = DomainErrorCodes.ClientPathFilterNotFound.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}