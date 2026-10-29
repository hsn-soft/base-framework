using HsnSoft.Base.Domain.Repositories;

namespace Edx.Mock.Center.Domain.Envelopes;

public interface IEnvelopeQueueRepository : IRepository<EnvelopeQueueItem, Guid>
{
    /// <summary>Adds the envelope only when no envelope of that uuid is there: the store decides (a unique index), so of two that arrive at the same time one is added. <see langword="false"/> means somebody was first.</summary>
    Task<bool> TryAddAsync(EnvelopeQueueItem item, CancellationToken cancellationToken = default);

    Task<EnvelopeQueueItem?> FindByEnvelopeIdAsync(string envelopeId, CancellationToken cancellationToken = default);

    /// <summary>The items at a stage, the oldest first, at most <paramref name="maxCount"/>.</summary>
    Task<IReadOnlyList<EnvelopeQueueItem>> GetByStageAsync(EnvelopeStage stage, int maxCount, CancellationToken cancellationToken = default);

    /// <summary>Whether another envelope that still holds its documents (not refused, not given up, not refused by the post box) has one of these keys: an invoice sent before.</summary>
    Task<bool> AnyHoldingDocumentAsync(IReadOnlyCollection<string> documentKeys, Guid exceptId, CancellationToken cancellationToken = default);

    /// <summary>The envelopes that were delivered and wait for the answer of the post box (1220), delivered before <paramref name="deliveredBefore"/>, at most <paramref name="maxCount"/>.</summary>
    Task<IReadOnlyList<EnvelopeQueueItem>> GetAwaitingAnswerAsync(DateTimeOffset deliveredBefore, int maxCount, CancellationToken cancellationToken = default);

    /// <summary>The items waiting to be delivered whose time has come, the oldest first, at most <paramref name="maxCount"/>.</summary>
    Task<IReadOnlyList<EnvelopeQueueItem>> GetDueForDeliveryAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken = default);
}
