using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;

public interface IInboundEnvelopeRepository : IRepository<InboundEnvelope, Guid>
{
    /// <summary>Adds the envelope only when none of that uuid is there: the store decides (a unique index). <see langword="false"/> means it was there.</summary>
    Task<bool> TryAddAsync(InboundEnvelope envelope, CancellationToken cancellationToken = default);

    Task<InboundEnvelope?> FindByEnvelopeIdAsync(string envelopeId, CancellationToken cancellationToken = default);

    /// <summary>The envelopes that came in and have not been processed, the oldest first, at most <paramref name="maxCount"/>.</summary>
    Task<IReadOnlyList<InboundEnvelope>> GetQueuedAsync(int maxCount, CancellationToken cancellationToken = default);

    /// <summary>Whether an invoice with this uuid was already taken (an envelope that was accepted has it): the same invoice is not taken twice.</summary>
    /// <summary>The invoice envelopes this post box took that the center has not ended yet, not asked about since <paramref name="checkedBefore"/>.</summary>
    Task<IReadOnlyList<InboundEnvelope>> GetAwaitingCenterStatusAsync(DateTimeOffset checkedBefore, int maxCount, CancellationToken cancellationToken = default);

    Task<bool> AnyAcceptedInvoiceAsync(Guid invoiceUuid, Guid exceptId, CancellationToken cancellationToken = default);
}
