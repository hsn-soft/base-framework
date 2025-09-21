using System;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

public abstract class BaseMongoDbContext(MongoClientSettings clientSettings, string databaseName) : MongoDbContext(clientSettings, databaseName)
{
    protected BaseMongoDbContext(string connectionString) : this(CreateClientSettings(connectionString), MongoUrl.Create(connectionString).DatabaseName)
    {
        CommandTrackerEvent += CommandTrackerEvent_Tracked;
    }

    private static MongoClientSettings CreateClientSettings(string connectionString, int queryExecutionMaxSeconds = 60)
    {
        var mongoUrl = MongoUrl.Create(connectionString);
        var clientSettings = MongoClientSettings.FromConnectionString(mongoUrl.Url);

        clientSettings.MaxConnectionPoolSize = 1000;
        clientSettings.MinConnectionPoolSize = 5;

        if (queryExecutionMaxSeconds < 1) queryExecutionMaxSeconds = 60;
        clientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(queryExecutionMaxSeconds);

        return clientSettings;
    }

    private static void CommandTrackerEvent_Tracked(object sender, MongoEntityEventArgs e)
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
            case MongoEntityEventState.Unchanged:
            default:
                break;
        }
    }

    private static void ApplyBaseConceptsForAddedEntity(object entity)
    {
        CheckAndSetId(entity);
        if (entity is not IAuditedObject objectWithCreationTime)
        {
            return;
        }

        if (objectWithCreationTime.CreationTime != default)
        {
            return;
        }

        ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.CreationTime, () => DateTime.UtcNow);
        ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.LastModificationTime, () => objectWithCreationTime.CreationTime);
    }

    private static void ApplyBaseConceptsForModifiedEntity(object entity)
    {
        if (entity is not IAuditedObject objectWithCreationTime)
        {
            return;
        }

        if (objectWithCreationTime.LastModificationTime == default)
        {
            ObjectHelper.TrySetProperty(objectWithCreationTime, x => x.LastModificationTime, () => DateTime.UtcNow);
        }
    }

    private static void CheckAndSetId(object targetObject)
    {
        if (targetObject is IEntity<Guid> entityWithGuidId)
        {
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
}