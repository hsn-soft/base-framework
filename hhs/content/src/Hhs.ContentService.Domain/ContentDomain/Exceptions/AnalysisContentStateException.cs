using HsnSoft.Base;
using HsnSoft.Base.Validation.Localization;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.Domain.ContentDomain.Exceptions;

[Serializable]
public sealed class AnalysisContentStateException : BusinessException
{
    public AnalysisContentStateException(IStringLocalizer localizer, string referenceCode)
        : base(errorMessage: localizer[DomainErrorCodes.AnalysisContentStateError])
    {
        ErrorCode = DomainErrorCodes.AnalysisContentStateError.Split(':').LastOrDefault();
        WithData(localizer[ValidationResourceKeys.ErrorReference], referenceCode);
    }
}