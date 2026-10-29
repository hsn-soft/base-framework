using Edx.Mock.Center.Domain.Envelopes;
using Edx.Shared.Efatura;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using MongoDB.Driver;

namespace Edx.Mock.Center.MongoDB;

/// <summary>The envelope queue in MongoDB: what <see cref="MongoRepository{TEntity, TKey}"/> does for any entity, and the add that the unique index of the envelope uuid decides.</summary>
public sealed class MongoEnvelopeQueueRepository(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName)
    : MongoRepository<EnvelopeQueueItem, Guid>(database, concepts, collectionName), IEnvelopeQueueRepository
{
    public async Task<bool> TryAddAsync(EnvelopeQueueItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await AddAsync(item, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public Task<EnvelopeQueueItem?> FindByEnvelopeIdAsync(string envelopeId, CancellationToken cancellationToken = default)
        => FindAsync(item => item.EnvelopeId == envelopeId, cancellationToken);

    public async Task<bool> AnyHoldingDocumentAsync(IReadOnlyCollection<string> documentKeys, Guid exceptId, CancellationToken cancellationToken = default)
    {
        if (documentKeys.Count == 0)
        {
            return false;
        }

        int[] holding = [EfaturaStatus.Queued, EfaturaStatus.Processing, EfaturaStatus.Processed, EfaturaStatus.NotDeliveredRetrying, EfaturaStatus.NoSystemResponse, EfaturaStatus.Completed];
        FilterDefinition<EnvelopeQueueItem> filter = Builders<EnvelopeQueueItem>.Filter.And(
            Builders<EnvelopeQueueItem>.Filter.AnyIn(item => item.DocumentKeys, documentKeys),
            Builders<EnvelopeQueueItem>.Filter.Ne(item => item.Id, exceptId),
            Builders<EnvelopeQueueItem>.Filter.In(item => item.StatusCode, holding));
        return await Collection.Find(filter).AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EnvelopeQueueItem>> GetAwaitingAnswerAsync(DateTimeOffset deliveredBefore, int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(
            new ListQueryOptions<EnvelopeQueueItem>
            {
                Filter = item => item.Origin == EnvelopeOrigin.Received && item.Stage == EnvelopeStage.Delivered && item.StatusCode == EfaturaStatus.NoSystemResponse && item.LastSendAt <= deliveredBefore,
                MaxResultCount = maxCount,
            },
            cancellationToken).ConfigureAwait(false)).OrderBy(item => item.ReceivedAt)];

    public async Task<IReadOnlyList<EnvelopeQueueItem>> GetByStageAsync(EnvelopeStage stage, int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(new ListQueryOptions<EnvelopeQueueItem> { Filter = item => item.Stage == stage, MaxResultCount = maxCount }, cancellationToken).ConfigureAwait(false)).OrderBy(item => item.ReceivedAt)];

    public async Task<IReadOnlyList<EnvelopeQueueItem>> GetDueForDeliveryAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken = default)
        => [.. (await GetListAsync(new ListQueryOptions<EnvelopeQueueItem> { Filter = item => item.Stage == EnvelopeStage.ToDeliver && item.NextSendAt <= now, MaxResultCount = maxCount }, cancellationToken).ConfigureAwait(false)).OrderBy(item => item.ReceivedAt)];
}
