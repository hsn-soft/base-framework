using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace HsnSoft.Base.MongoDB;

public abstract class BaseMongoDbContext : MongoDbContext
{
    [CanBeNull] private IAuditPropertySetter AuditPropertySetter { get; }

    public TimeSpan ClientWaitQueueTimeout => Client.Settings.WaitQueueTimeout;

    protected BaseMongoDbContext(MongoClientSettings clientSettings, [NotNull] string databaseName, [CanBeNull] IServiceProvider provider = null) : base(clientSettings, databaseName)
    {
        AuditPropertySetter = provider?.GetService<IAuditPropertySetter>();
        CommandTrackerEvent += CommandTrackerEvent_Tracked;
    }

    protected BaseMongoDbContext([NotNull] string connectionString, [CanBeNull] IServiceProvider provider = null) : this(CreateClientSettings(connectionString, provider: provider), MongoUrl.Create(connectionString).DatabaseName, provider)
    {
    }

    public  IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default)
        => StartSessionAsync(options, cancellationToken).GetAwaiter().GetResult();

    public async  Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default)
        => await Client.StartSessionAsync(options, cancellationToken);

    private static MongoClientSettings CreateClientSettings([NotNull] string connectionString, int queryExecutionMaxSeconds = 60, IServiceProvider provider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var mongoUrl = MongoUrl.Create(connectionString);
        var clientSettings = MongoClientSettings.FromConnectionString(mongoUrl.Url);

        clientSettings.MaxConnectionPoolSize = 1000;
        clientSettings.MinConnectionPoolSize = 5;

        if (queryExecutionMaxSeconds < 1) queryExecutionMaxSeconds = 60;
        clientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(queryExecutionMaxSeconds);


        bool isDefaultLoggerActive = false;
        var loggerFactory = provider?.GetService<ILoggerFactory>();
        if (loggerFactory == null)
        {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            isDefaultLoggerActive = true;
        }

        clientSettings.LoggingSettings = new LoggingSettings(loggerFactory);

        var logger = loggerFactory.CreateLogger<MongoDbContext>();
        clientSettings.ClusterConfigurator = cb =>
        {
            cb.Subscribe<CommandStartedEvent>(e =>
            {
                if (isDefaultLoggerActive)
                    logger.LogInformation("Mongo Command Started: {CommandName} - {Command}", e.CommandName, e.Command.ToJson());
                else
                    logger.LogDebug("Mongo Command Started: {CommandName} - {Command}", e.CommandName, e.Command.ToJson());
            });
            cb.Subscribe<CommandSucceededEvent>(e =>
            {
                if (isDefaultLoggerActive)
                    logger.LogInformation("Mongo Command Succeeded: {CommandName} - Duration: {Duration}ms", e.CommandName, e.Duration.TotalMilliseconds);
                else
                    logger.LogDebug("Mongo Command Succeeded: {CommandName} - Duration: {Duration}ms", e.CommandName, e.Duration.TotalMilliseconds);
            });
            cb.Subscribe<CommandFailedEvent>(e => { logger.LogError(e.Failure, "Mongo Command Failed: {CommandName}", e.CommandName); });
        };

        return clientSettings;
    }

    private void CommandTrackerEvent_Tracked(object sender, MongoEntityEventArgs e)
    {
        switch (e.EventState)
        {
            case MongoEntityEventState.Added:
                ApplyBaseConceptsForAddedEntity(e.EntryEntity);
                break;
            case MongoEntityEventState.Modified:
                ApplyBaseConceptsForModifiedEntity(e.EntryEntity);
                break;
            case MongoEntityEventState.Deleted:
                ApplyBaseConceptsForDeletedEntity(e.EntryEntity);
                break;
            case MongoEntityEventState.Unchanged:
            default:
                break;
        }
    }

    private void ApplyBaseConceptsForAddedEntity(object entity)
    {
        CheckAndSetId(entity);
        AuditPropertySetter?.SetCreationProperties(entity);
    }

    private void ApplyBaseConceptsForModifiedEntity(object entity)
    {
        AuditPropertySetter?.SetModificationProperties(entity);
        if (entity is ISoftDelete { IsDeleted: true } softDelete)
        {
            AuditPropertySetter?.SetDeletionProperties(softDelete);
        }
    }

    private void ApplyBaseConceptsForDeletedEntity(object entity)
    {
        if (entity is not ISoftDelete softDelete)
        {
            return;
        }

        ObjectHelper.TrySetProperty(softDelete, x => x.IsDeleted, () => true);
        AuditPropertySetter?.SetDeletionProperties(softDelete);

        // SoftDeletion Active and DeletionProperties not found then Set modification properties
        if (softDelete is not IHasDeletionTime)
        {
            AuditPropertySetter?.SetModificationProperties(softDelete);
        }
    }

    private static void CheckAndSetId(object targetObject)
    {
        if (targetObject is not IEntity<Guid> entityWithGuidId)
        {
            return;
        }

        if (entityWithGuidId.Id != Guid.Empty)
        {
            return;
        }

        EntityHelper.TrySetId(
            entityWithGuidId,
            Guid.NewGuid,
            true
        );
    }
}