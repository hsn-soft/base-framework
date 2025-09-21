using System;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.MongoDB.Attributes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace HsnSoft.Base.MongoDB.Context;

public abstract class MongoDbContext : IDisposable
{
    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }

    [CanBeNull] protected event EventHandler<MongoEntityEventArgs> CommandTrackerEvent;
    private bool _disposed;

    protected MongoDbContext(MongoClientSettings clientSettings, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(clientSettings);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        }).CreateLogger<MongoDbContext>();
        clientSettings.ClusterConfigurator = cb =>
        {
            cb.Subscribe<CommandStartedEvent>(e => { logger.LogDebug("Mongo Command Started: {CommandName} - {Command}", e.CommandName, e.Command.ToJson()); });
            cb.Subscribe<CommandSucceededEvent>(e => { logger.LogDebug("Mongo Command Succeeded: {CommandName} - Duration: {Duration}ms", e.CommandName, e.Duration.TotalMilliseconds); });
            cb.Subscribe<CommandFailedEvent>(e => { logger.LogError(e.Failure, "Mongo Command Failed: {CommandName}", e.CommandName); });
        };

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