using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// The startup lock of MongoDB, which has no lock of a session: a lease. A document of the collection <c>_startup_locks</c> of the database is the lock; one process holds it by writing its own name and the time its lease ends,
/// renews it while it holds it, and deletes it when it is done. A process that dies stops renewing, so the lease ends and another takes the lock over (by default the lease is a minute, renewed every twenty seconds).
/// </summary>
public sealed class MongoStartupLock : IStartupLock
{
    /// <summary>The collection of the lock documents.</summary>
    public const string CollectionName = "_startup_locks";

    private static readonly TimeSpan s_poll = TimeSpan.FromMilliseconds(500);

    private readonly IMongoCollection<BsonDocument> _locks;
    private readonly TimeSpan _lease;
    private readonly TimeSpan _renew;

    /// <summary>The lock with the times of the framework: a lease of a minute, renewed every twenty seconds.</summary>
    public MongoStartupLock(IMongoDatabase database)
        : this(database, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(20))
    {
    }

    /// <param name="database">The database whose collection <see cref="CollectionName"/> holds the lock documents.</param>
    /// <param name="lease">How long a holder owns the lock without renewing it: a holder that died is taken over after this.</param>
    /// <param name="renew">How often a holder renews its lease; a third of the lease or less.</param>
    public MongoStartupLock(IMongoDatabase database, TimeSpan lease, TimeSpan renew)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lease, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(renew, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(renew, lease);

        _locks = database.GetCollection<BsonDocument>(CollectionName);
        _lease = lease;
        _renew = renew;
    }

    public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
    {
        string owner = Guid.NewGuid().ToString("N");
        DateTime deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            DateTime now = DateTime.UtcNow;
            // a lock that is free (no document) or whose lease has ended: the upsert writes it. One that is held: the filter does not match, the upsert tries to insert the same _id, and the duplicate says "held".
            FilterDefinition<BsonDocument> free = Builders<BsonDocument>.Filter.Eq("_id", name) & Builders<BsonDocument>.Filter.Lt("expiresAt", now);
            UpdateDefinition<BsonDocument> take = Builders<BsonDocument>.Update.Set("owner", owner).Set("expiresAt", now + _lease);
            try
            {
                await _locks.UpdateOneAsync(free, take, new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                return new Held(_locks, name, owner, _lease, _renew);
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
        private readonly TimeSpan _lease;
        private readonly TimeSpan _renew;
        private readonly IMongoCollection<BsonDocument> _locks;
        private readonly string _name;
        private readonly string _owner;
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _renewing;

        public Held(IMongoCollection<BsonDocument> locks, string name, string owner, TimeSpan lease, TimeSpan renew)
        {
            _lease = lease;
            _renew = renew;
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
                    await Task.Delay(_renew, cancellationToken).ConfigureAwait(false);
                    try
                    {
                        await _locks.UpdateOneAsync(Mine, Builders<BsonDocument>.Update.Set("expiresAt", DateTime.UtcNow + _lease), cancellationToken: cancellationToken).ConfigureAwait(false);
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
