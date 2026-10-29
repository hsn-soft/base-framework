using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.Integration.GibInvoice.MongoDB;

/// <summary>The inbound envelopes in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the queries of the workers.</summary>
public sealed class MongoInboundEnvelopeRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<InboundEnvelope, Guid>(database, concepts, collectionName), IInboundEnvelopeRepository
{
    public async Task<bool> TryAddAsync(InboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAsync(envelope, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public Task<InboundEnvelope?> FindByEnvelopeIdAsync(string envelopeId, CancellationToken cancellationToken = default)
        => FindAsync(envelope => envelope.EnvelopeId == envelopeId, cancellationToken);

    public async Task<IReadOnlyList<InboundEnvelope>> GetQueuedAsync(int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(new ListQueryOptions<InboundEnvelope> { Filter = envelope => envelope.State == InboundState.Queued, MaxResultCount = maxCount }, cancellationToken).ConfigureAwait(false)).OrderBy(envelope => envelope.ReceivedAt)];

    public async Task<IReadOnlyList<InboundEnvelope>> GetAwaitingCenterStatusAsync(DateTimeOffset checkedBefore, int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(
                new ListQueryOptions<InboundEnvelope> { Filter = envelope => envelope.State == InboundState.Accepted && (envelope.CenterCheckedAt == null || envelope.CenterCheckedAt < checkedBefore), MaxResultCount = maxCount * 4 },
                cancellationToken).ConfigureAwait(false)).Where(envelope => envelope.AwaitsCenterStatus).OrderBy(envelope => envelope.ReceivedAt).Take(maxCount)];

    public Task<bool> AnyAcceptedInvoiceAsync(Guid invoiceUuid, Guid exceptId, CancellationToken cancellationToken = default)
        => AnyAsync(envelope => envelope.InvoiceUuid == invoiceUuid && envelope.State == InboundState.Accepted && envelope.Id != exceptId, ignoreFilters: false, cancellationToken);
}
