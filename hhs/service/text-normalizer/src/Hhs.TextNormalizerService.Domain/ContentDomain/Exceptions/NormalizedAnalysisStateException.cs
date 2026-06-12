using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class NormalizedAnalysisStateException : BusinessException
{
    public NormalizedAnalysisStateException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.NormalizedAnalysisStateError)
    {
        ErrorCode = DomainErrorCodes.NormalizedAnalysisStateError.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}