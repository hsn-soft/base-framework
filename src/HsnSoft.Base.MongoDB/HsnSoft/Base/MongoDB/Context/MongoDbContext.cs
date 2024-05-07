using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Attributes;
using JetBrains.Annotations;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Context;

public abstract class MongoDbContext
{
    private static readonly object DbResourceLock = new();
    private readonly List<Func<object, Task<object>>> _commands;
    protected IMongoClient Client { get; private set; }

    protected IMongoDatabase Database { get; private set; }

    [CanBeNull]
    public event EventHandler<MongoEntityEventArgs> CommandTrackerEvent;

    protected MongoDbContext(MongoClientSettings clientSettings, string databaseName)
    {
        Client = new MongoClient(clientSettings);
        Database = Client.GetDatabase(databaseName);

        // Every command will be stored and it'll be processed at SaveChanges
        _commands = new List<Func<object?, Task<object?>>>();
    }

    public IMongoCollection<TEntity> Collection<TEntity>() where TEntity : class, IEntity
    {
        return Database.GetCollection<TEntity>(GetCollectionName(typeof(TEntity)));
    }

    public void ApplyEntityTrackingChanges<TEntity>(TEntity obj, MongoCommandState commandState)
    {
        CommandTrackerEvent?.Invoke(this, new MongoEntityEventArgs { CommandState = commandState, EntryEntity = obj });
    }

    public Task AddEntityCommandAsync(Func<object?, Task<object?>> func)
    {
        lock (DbResourceLock)
        {
            _commands.Add(func);

            return Task.CompletedTask;
        }
    }

    protected int SaveEntityCommands() => SaveEntityCommandsAsync().GetAwaiter().GetResult();

    protected Task<int> SaveEntityCommandsAsync()
    {
        lock (DbResourceLock)
        {
            return Task.FromResult(SaveEntityCommandsAsync(_commands).GetAwaiter().GetResult());
        }
    }

    private async Task<int> SaveEntityCommandsAsync(IEnumerable<Func<object?, Task<object?>>> commands)
    {
        var requestCommandCount = commands.Count();
        if (requestCommandCount < 1) return int.MaxValue; // for success result

        using var session = await Client.StartSessionAsync();
        var isServerSupportTransaction = true;
        try
        {
            session.StartTransaction();
        }
        catch (NotSupportedException e)
        {
            isServerSupportTransaction = false;
        }

        var commandTasks = commands.Select(c => c(null));

        await Task.WhenAll(commandTasks);

        if (isServerSupportTransaction)
        {
            await session.CommitTransactionAsync();
        }

        var resultCommandCount = commands.Count();
        _commands.Clear();

        return resultCommandCount;
    }

    private static string GetCollectionName(MemberInfo entityType)
        => ((BsonCollectionAttribute)entityType.GetCustomAttributes(typeof(BsonCollectionAttribute), true)
               .FirstOrDefault())?.CollectionName
           ?? entityType.Name;
}