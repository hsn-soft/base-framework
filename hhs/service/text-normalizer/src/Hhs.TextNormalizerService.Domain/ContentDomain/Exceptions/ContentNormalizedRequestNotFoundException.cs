using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class ContentNormalizedRequestNotFoundException : BusinessException
{
    public ContentNormalizedRequestNotFoundException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.ContentNormalizedRequestNotFound)
    {
        ErrorCode = DomainErrorCodes.ContentNormalizedRequestNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}