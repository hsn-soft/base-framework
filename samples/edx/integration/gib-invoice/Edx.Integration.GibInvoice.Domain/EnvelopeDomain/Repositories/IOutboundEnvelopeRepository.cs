using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;

public interface IOutboundEnvelopeRepository : IRepository<OutboundEnvelope, Guid>
{
    /// <summary>Adds the envelope only when none of that uuid is there: the store decides (a unique index). <see langword="false"/> means it was there.</summary>
    Task<bool> TryAddAsync(OutboundEnvelope envelope, CancellationToken cancellationToken = default);

    Task<OutboundEnvelope?> FindByEnvelopeIdAsync(string envelopeId, CancellationToken cancellationToken = default);

    /// <summary>The envelopes waiting to be sent whose time has come, the oldest first, at most <paramref name="maxCount"/> (all tenants' when the caller is the system tenant).</summary>
    Task<IReadOnlyList<OutboundEnvelope>> GetDueToSendAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken = default);

    /// <summary>The invoice envelopes the center took and whose road is not at its end, last heard of before <paramref name="heardBefore"/>, the oldest first.</summary>
    Task<IReadOnlyList<OutboundEnvelope>> GetAwaitingStatusAsync(DateTimeOffset heardBefore, int maxCount, CancellationToken cancellationToken = default);
}
