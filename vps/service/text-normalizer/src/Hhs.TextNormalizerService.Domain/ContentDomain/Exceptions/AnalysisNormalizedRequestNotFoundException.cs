using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class AnalysisNormalizedRequestNotFoundException : BusinessException
{
    public AnalysisNormalizedRequestNotFoundException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.AnalysisNormalizedRequestNotFound)
    {
        ErrorCode = DomainErrorCodes.AnalysisNormalizedRequestNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}