using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class NormalizedRequestStateException : BusinessException
{
    public NormalizedRequestStateException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.NormalizedRequestStateError)
    {
        ErrorCode = DomainErrorCodes.NormalizedRequestStateError.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}