using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ContentDomain.Exceptions;

[Serializable]
public class AnalysisContentItemNotFoundException : BusinessException
{
    public AnalysisContentItemNotFoundException(IStringLocalizer localizer, Guid id)
        : base(errorMessage: localizer[DomainErrorCodes.AnalysisContentNotFound])
    {
        ErrorCode = DomainErrorCodes.AnalysisContentNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, id.ToString());
    }
}
