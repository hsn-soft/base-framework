using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class ContentNormalizedRequestStateException : BusinessException
{
    public ContentNormalizedRequestStateException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.ContentNormalizedRequestStateError)
    {
        ErrorCode = DomainErrorCodes.ContentNormalizedRequestStateError.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}