using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class AnalysisNormalizedRequestStateException : BusinessException
{
    public AnalysisNormalizedRequestStateException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.AnalysisNormalizedRequestStateError)
    {
        ErrorCode = DomainErrorCodes.AnalysisNormalizedRequestStateError.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}