using HsnSoft.Base;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Exceptions;

public sealed class CustomerContentNormalizedRequestNotFoundException : BusinessException
{
    public CustomerContentNormalizedRequestNotFoundException(Guid id)
        : base($"CustomerContentNormalizedRequest with id '{id}' not found.")
    {
    }

    public CustomerContentNormalizedRequestNotFoundException(string scopeKey, Guid customerContentId)
        : base($"CustomerContentNormalizedRequest with scopeKey '{scopeKey}' and customerContentId '{customerContentId}' not found.")
    {
    }
}
