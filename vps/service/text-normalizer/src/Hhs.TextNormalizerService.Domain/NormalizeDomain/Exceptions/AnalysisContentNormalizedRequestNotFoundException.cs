using HsnSoft.Base;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Exceptions;

public sealed class AnalysisContentNormalizedRequestNotFoundException : BusinessException
{
    public AnalysisContentNormalizedRequestNotFoundException(Guid id)
        : base($"AnalysisContentNormalizedRequest with id '{id}' not found.")
    {
    }

    public AnalysisContentNormalizedRequestNotFoundException(string scopeKey, Guid analysisContentId)
        : base($"AnalysisContentNormalizedRequest with scopeKey '{scopeKey}' and analysisContentId '{analysisContentId}' not found.")
    {
    }
}
