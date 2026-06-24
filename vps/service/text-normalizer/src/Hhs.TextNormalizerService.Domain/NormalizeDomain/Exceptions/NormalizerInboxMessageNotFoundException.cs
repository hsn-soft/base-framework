using HsnSoft.Base;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Exceptions;

public sealed class NormalizerInboxMessageNotFoundException : BusinessException
{
    public NormalizerInboxMessageNotFoundException(Guid eventId)
        : base($"NormalizerInboxMessage with eventId '{eventId}' not found.")
    {
    }
}
