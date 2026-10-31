using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// The startup lock of MongoDB, which has no lock of a session: a lease. A document of the collection <c>_startup_locks</c> of the database is the lock; one process holds it by writing its own name and the time its lease ends,
/// renews it while it holds it, and deletes it when it is done. A process that dies stops renewing, so the lease ends and another takes the lock over (the lease is a minute, renewed every twenty seconds).
/// </summary>
public sealed class MongoStartupLock(IMongoDatabase database) : IStartupLock
{
    /// <summary>The collection of the lock documents.</summary>
    public const string CollectionName = "_startup_locks";

    private static readonly TimeSpan s_lease = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan s_renew = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan s_poll = TimeSpan.FromMilliseconds(500);

    private readonly IMongoCollection<BsonDocument> _locks = database.GetCollection<BsonDocument>(CollectionName);

    public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
    {
        string owner = Guid.NewGuid().ToString("N");
        DateTime deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            DateTime now = DateTime.UtcNow;
            // a lock that is free (no document) or whose lease has ended: the upsert writes it. One that is held: the filter does not match, the upsert tries to insert the same _id, and the duplicate says "held".
            FilterDefinition<BsonDocument> free = Builders<BsonDocument>.Filter.Eq("_id", name) & Builders<BsonDocument>.Filter.Lt("expiresAt", now);
            UpdateDefinition<BsonDocument> take = Builders<BsonDocument>.Update.Set("owner", owner).Set("expiresAt", now + s_lease);
            try
            {
                await _locks.UpdateOneAsync(free, take, new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                return new Held(_locks, name, owner);
            }
            catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // held by another
            }
            catch (MongoCommandException exception) when (exception.Code == 11000)
            {
                // held by another
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"The startup lock '{name}' was not free within {timeout}.");
            }

            await Task.Delay(s_poll, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class Held : IAsyncDisposable
    {
        private readonly IMongoCollection<BsonDocument> _locks;
        private readonly string _name;
        private readonly string _owner;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _renewing;

        public Held(IMongoCollection<BsonDocument> locks, string name, string owner)
        {
            _locks = locks;
            _name = name;
            _owner = owner;
            _renewing = RenewAsync(_stop.Token);
        }

        private FilterDefinition<BsonDocument> Mine => Builders<BsonDocument>.Filter.Eq("_id", _name) & Builders<BsonDocument>.Filter.Eq("owner", _owner);

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync().ConfigureAwait(false);
            await _renewing.ConfigureAwait(false);
            _stop.Dispose();
            try
            {
                await _locks.DeleteOneAsync(Mine).ConfigureAwait(false);
            }
            catch (MongoException)
            {
                // the lease ends by itself
            }
        }

        private async Task RenewAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(s_renew, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await _locks.UpdateOneAsync(Mine, Builders<BsonDocument>.Update.Set("expiresAt", DateTime.UtcNow + s_lease), cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoException)
                    {
                        // a renewal that fails is tried again at the next one; the lease is three of them long
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // released
            }
        }
    }
}
