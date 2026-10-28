using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.Integration.GibInvoice.MongoDB;

/// <summary>The outbound envelopes in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the queries of the workers.</summary>
public sealed class MongoOutboundEnvelopeRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<OutboundEnvelope, Guid>(database, concepts, collectionName), IOutboundEnvelopeRepository
{
    public async Task<bool> TryAddAsync(OutboundEnvelope envelope, CancellationToken cancellationToken = default)
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

    public Task<OutboundEnvelope?> FindByEnvelopeIdAsync(string envelopeId, CancellationToken cancellationToken = default)
        => FindAsync(envelope => envelope.EnvelopeId == envelopeId, cancellationToken);

    public async Task<IReadOnlyList<OutboundEnvelope>> GetDueToSendAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(new ListQueryOptions<OutboundEnvelope> { Filter = envelope => envelope.State == OutboundState.ToSend && envelope.NextSendAt <= now, MaxResultCount = maxCount }, cancellationToken).ConfigureAwait(false)).OrderBy(envelope => envelope.CreatedAt)];

    public async Task<IReadOnlyList<OutboundEnvelope>> GetAwaitingStatusAsync(DateTimeOffset heardBefore, int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(
            new ListQueryOptions<OutboundEnvelope>
            {
                Filter = envelope => envelope.Kind != OutboundKind.SystemResponse && envelope.State == OutboundState.Sent && envelope.StatusChangedAt <= heardBefore,
                MaxResultCount = maxCount,
            },
            cancellationToken).ConfigureAwait(false)).OrderBy(envelope => envelope.CreatedAt)];
}
