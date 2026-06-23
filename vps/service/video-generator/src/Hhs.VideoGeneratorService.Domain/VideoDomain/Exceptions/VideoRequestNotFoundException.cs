using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;

namespace Hhs.VideoGeneratorService.Domain.VideoDomain.Exceptions;

[Serializable]
public sealed class VideoRequestNotFoundException : BusinessException
{
    public VideoRequestNotFoundException(string referenceCode)
        : base(errorMessage: DomainErrorCodes.VideoRequestNotFound)
    {
        ErrorCode = DomainErrorCodes.VideoRequestNotFound.Split(':').LastOrDefault();
        WithData(ValidationResourceKeys.ErrorReference, referenceCode);
    }
}