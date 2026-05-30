using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class NormalizedAnalysisNotFoundException : BusinessException
{
    public NormalizedAnalysisNotFoundException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.NormalizedAnalysisNotFound)
    {
        ErrorCode = DomainErrorCodes.NormalizedAnalysisNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}