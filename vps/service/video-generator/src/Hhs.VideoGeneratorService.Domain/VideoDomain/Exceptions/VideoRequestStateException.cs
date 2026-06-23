using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.VideoGeneratorService.Domain.VideoDomain.Exceptions;

[Serializable]
public sealed class VideoRequestStateException : BusinessException
{
    public VideoRequestStateException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.VideoRequestStateError)
    {
        ErrorCode = DomainErrorCodes.VideoRequestStateError.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}