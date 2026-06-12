using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.TextNormalizerService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class NormalizedRequestNotFoundException : BusinessException
{
    public NormalizedRequestNotFoundException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.NormalizedRequestNotFound)
    {
        ErrorCode = DomainErrorCodes.NormalizedRequestNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}