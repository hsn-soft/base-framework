using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ContentDomain.Exceptions;

[Serializable]
public class CustomerContentNotFoundException : BusinessException
{
    public CustomerContentNotFoundException(IStringLocalizer localizer, Guid id)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentNotFound])
    {
        ErrorCode = DomainErrorCodes.CustomerContentNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, id.ToString());
    }

    public CustomerContentNotFoundException(IStringLocalizer localizer, string scopeKey, string domainName)
        : base(errorMessage: localizer[DomainErrorCodes.CustomerContentNotFound])
    {
        ErrorCode = DomainErrorCodes.CustomerContentNotFound.Split(':').LastOrDefault();
        WithData("ScopeKey", scopeKey);
        WithData("DomainName", domainName);
    }
}