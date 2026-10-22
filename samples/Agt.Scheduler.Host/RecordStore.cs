using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Agt.Scheduler.Host;

public sealed class RecordStore(IMongoDatabase database, IOptions<SchedulerOptions> options)
{
    private readonly IMongoCollection<SampleRecord> _records = database.GetCollection<SampleRecord>(options.Value.Collection);

    /// <summary>The filter of every check is on the creation time, so it gets an index.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
        => await _records.Indexes.CreateOneAsync(
            new CreateIndexModel<SampleRecord>(Builders<SampleRecord>.IndexKeys.Descending(x => x.CreatedAt)),
            cancellationToken: cancellationToken);

    /// <summary>The ids of the records created at or after <paramref name="since"/> (UTC), oldest first, at most <paramref name="limit"/>.</summary>
    public async Task<List<ObjectId>> FindIdsCreatedSinceAsync(DateTime since, int limit, CancellationToken cancellationToken)
        => await _records
            .Find(x => x.CreatedAt >= since)
            .SortBy(x => x.CreatedAt)
            .Limit(limit)
            .Project(x => x.Id)
            .ToListAsync(cancellationToken);
}
