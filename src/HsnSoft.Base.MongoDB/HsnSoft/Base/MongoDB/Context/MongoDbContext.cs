using System;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.MongoDB.Attributes;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Context;

public abstract class MongoDbContext : IDisposable
{
    protected IMongoClient Client { get; }
    protected IMongoDatabase Database { get; }

    [CanBeNull] protected event EventHandler<MongoEntityEventArgs> CommandTrackerEvent;
    private bool _disposed;

    protected MongoDbContext(MongoClientSettings clientSettings, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(clientSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        Client = new MongoClient(clientSettings);
        Database = Client.GetDatabase(databaseName);
    }

    public IMongoCollection<TEntity> GetCollection<TEntity>()
        => new TrackingMongoCollection<TEntity>(Database.GetCollection<TEntity>(GetCollectionName(typeof(TEntity))), CommandTrackerEvent);

    private static string GetCollectionName(MemberInfo entityType)
        => ((BsonCollectionAttribute)entityType.GetCustomAttributes(typeof(BsonCollectionAttribute), true)
               .FirstOrDefault())?.CollectionName
           ?? entityType.Name;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // managed resources cleanup
            Client?.Dispose();
        }

        // unmanaged resources cleanup
        _disposed = true;
    }

    ~MongoDbContext()
    {
        Dispose(false);
    }
}