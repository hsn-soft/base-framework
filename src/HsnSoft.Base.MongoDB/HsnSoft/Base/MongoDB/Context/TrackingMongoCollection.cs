using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Auditing;
using JetBrains.Annotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Search;

namespace HsnSoft.Base.MongoDB.Context;

public interface ITrackingMongoCollection<TEntity> : IMongoCollection<TEntity>;

public class TrackingMongoCollection<TEntity>(IMongoCollection<TEntity> inner, [CanBeNull] EventHandler<MongoEntityEventArgs> commandTrackerEventHandler) : ITrackingMongoCollection<TEntity>
{
    [CanBeNull] private event EventHandler<MongoEntityEventArgs> CommandTrackerEventHandler = commandTrackerEventHandler;

    private static UpdateDefinition<TEntity> CheckAndSetModification(UpdateDefinition<TEntity> update)
    {
        if (typeof(TEntity).GetMember(nameof(IHasModificationTime.LastModificationTime)) is { Length: > 0 })
        {
            update = update.Set(nameof(IHasModificationTime.LastModificationTime), DateTime.UtcNow);
        }

        if (typeof(TEntity).GetMember(nameof(IModificationAuditedObject.LastModifierId)) is { Length: > 0 })
        {
            update = update.Set(nameof(IModificationAuditedObject.LastModifierId), BsonNull.Value);
        }

        if (typeof(TEntity).GetMember(nameof(IDeletionAuditedObject.DeleterId)) is { Length: > 0 })
        {
            update = update.Set(nameof(IDeletionAuditedObject.DeleterId), BsonNull.Value);
        }

        return update;
    }

    public IAsyncCursor<TResult> Aggregate<TResult>(PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.Aggregate(pipeline, options, cancellationToken);

    public IAsyncCursor<TResult> Aggregate<TResult>(IClientSessionHandle session, PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.Aggregate(session, pipeline, options, cancellationToken);

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.AggregateAsync(pipeline, options, cancellationToken);

    public Task<IAsyncCursor<TResult>> AggregateAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.AggregateAsync(session, pipeline, options, cancellationToken);

    public void AggregateToCollection<TResult>(PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.AggregateToCollection(pipeline, options, cancellationToken);

    public void AggregateToCollection<TResult>(IClientSessionHandle session, PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.AggregateToCollection(session, pipeline, options, cancellationToken);

    public Task AggregateToCollectionAsync<TResult>(PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.AggregateToCollectionAsync(pipeline, options, cancellationToken);

    public Task AggregateToCollectionAsync<TResult>(IClientSessionHandle session, PipelineDefinition<TEntity, TResult> pipeline, AggregateOptions options = null, CancellationToken cancellationToken = new())
        => inner.AggregateToCollectionAsync(session, pipeline, options, cancellationToken);

    public BulkWriteResult<TEntity> BulkWrite(IEnumerable<WriteModel<TEntity>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new())
        => inner.BulkWrite(requests, options, cancellationToken);

    public BulkWriteResult<TEntity> BulkWrite(IClientSessionHandle session, IEnumerable<WriteModel<TEntity>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new())
        => inner.BulkWrite(session, requests, options, cancellationToken);

    public Task<BulkWriteResult<TEntity>> BulkWriteAsync(IEnumerable<WriteModel<TEntity>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new())
        => inner.BulkWriteAsync(requests, options, cancellationToken);

    public Task<BulkWriteResult<TEntity>> BulkWriteAsync(IClientSessionHandle session, IEnumerable<WriteModel<TEntity>> requests, BulkWriteOptions options = null, CancellationToken cancellationToken = new())
        => inner.BulkWriteAsync(session, requests, options, cancellationToken);

    [Obsolete("Obsolete")]
    public long Count(FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.Count(filter, options, cancellationToken);

    [Obsolete("Obsolete")]
    public long Count(IClientSessionHandle session, FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.Count(session, filter, options, cancellationToken);

    [Obsolete("Obsolete")]
    public Task<long> CountAsync(FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.CountAsync(filter, options, cancellationToken);

    [Obsolete("Obsolete")]
    public Task<long> CountAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.CountAsync(session, filter, options, cancellationToken);

    public long CountDocuments(FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.CountDocuments(filter, options, cancellationToken);

    public long CountDocuments(IClientSessionHandle session, FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.CountDocuments(session, filter, options, cancellationToken);

    public Task<long> CountDocumentsAsync(FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.CountDocumentsAsync(filter, options, cancellationToken);

    public Task<long> CountDocumentsAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, CountOptions options = null, CancellationToken cancellationToken = new())
        => inner.CountDocumentsAsync(session, filter, options, cancellationToken);

    public DeleteResult DeleteMany(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = new())
        => inner.DeleteMany(filter, cancellationToken);

    public DeleteResult DeleteMany(FilterDefinition<TEntity> filter, DeleteOptions options, CancellationToken cancellationToken = new())
        => inner.DeleteMany(filter, options, cancellationToken);

    public DeleteResult DeleteMany(IClientSessionHandle session, FilterDefinition<TEntity> filter, DeleteOptions options = null, CancellationToken cancellationToken = new())
        => inner.DeleteMany(session, filter, options, cancellationToken);

    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = new())
        => inner.DeleteManyAsync(filter, cancellationToken);

    public Task<DeleteResult> DeleteManyAsync(FilterDefinition<TEntity> filter, DeleteOptions options, CancellationToken cancellationToken = new())
        => inner.DeleteManyAsync(filter, options, cancellationToken);

    public Task<DeleteResult> DeleteManyAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, DeleteOptions options = null, CancellationToken cancellationToken = new())
        => inner.DeleteManyAsync(session, filter, options, cancellationToken);

    public DeleteResult DeleteOne(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = new())
        => inner.DeleteOne(filter, cancellationToken);

    public DeleteResult DeleteOne(FilterDefinition<TEntity> filter, DeleteOptions options, CancellationToken cancellationToken = new())
        => inner.DeleteOne(filter, options, cancellationToken);

    public DeleteResult DeleteOne(IClientSessionHandle session, FilterDefinition<TEntity> filter, DeleteOptions options = null, CancellationToken cancellationToken = new())
        => inner.DeleteOne(session, filter, options, cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = new())
        => inner.DeleteOneAsync(filter, cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(FilterDefinition<TEntity> filter, DeleteOptions options, CancellationToken cancellationToken = new())
        => inner.DeleteOneAsync(filter, options, cancellationToken);

    public Task<DeleteResult> DeleteOneAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, DeleteOptions options = null, CancellationToken cancellationToken = new())
        => inner.DeleteOneAsync(session, filter, options, cancellationToken);

    public IAsyncCursor<TField> Distinct<TField>(FieldDefinition<TEntity, TField> field, FilterDefinition<TEntity> filter, DistinctOptions options = null, CancellationToken cancellationToken = new())
        => inner.Distinct(field, filter, options, cancellationToken);

    public IAsyncCursor<TField> Distinct<TField>(IClientSessionHandle session, FieldDefinition<TEntity, TField> field, FilterDefinition<TEntity> filter, DistinctOptions options = null, CancellationToken cancellationToken = new())
        => inner.Distinct(session, field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(FieldDefinition<TEntity, TField> field, FilterDefinition<TEntity> filter, DistinctOptions options = null, CancellationToken cancellationToken = new())
        => inner.DistinctAsync(field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TField>> DistinctAsync<TField>(IClientSessionHandle session, FieldDefinition<TEntity, TField> field, FilterDefinition<TEntity> filter, DistinctOptions options = null, CancellationToken cancellationToken = new())
        => inner.DistinctAsync(session, field, filter, options, cancellationToken);

    public IAsyncCursor<TItem> DistinctMany<TItem>(FieldDefinition<TEntity, IEnumerable<TItem>> field, FilterDefinition<TEntity> filter, DistinctOptions options = null, CancellationToken cancellationToken = new())
        => inner.DistinctMany(field, filter, options, cancellationToken);

    public IAsyncCursor<TItem> DistinctMany<TItem>(IClientSessionHandle session, FieldDefinition<TEntity, IEnumerable<TItem>> field, FilterDefinition<TEntity> filter, DistinctOptions options = null,
        CancellationToken cancellationToken = new())
        => inner.DistinctMany(session, field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(FieldDefinition<TEntity, IEnumerable<TItem>> field, FilterDefinition<TEntity> filter, DistinctOptions options = null, CancellationToken cancellationToken = new())
        => inner.DistinctManyAsync(field, filter, options, cancellationToken);

    public Task<IAsyncCursor<TItem>> DistinctManyAsync<TItem>(IClientSessionHandle session, FieldDefinition<TEntity, IEnumerable<TItem>> field, FilterDefinition<TEntity> filter, DistinctOptions options = null,
        CancellationToken cancellationToken = new())
        => inner.DistinctManyAsync(session, field, filter, options, cancellationToken);

    public long EstimatedDocumentCount(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = new())
        => inner.EstimatedDocumentCount(options, cancellationToken);

    public Task<long> EstimatedDocumentCountAsync(EstimatedDocumentCountOptions options = null, CancellationToken cancellationToken = new())
        => inner.EstimatedDocumentCountAsync(options, cancellationToken);

    public IAsyncCursor<TProjection> FindSync<TProjection>(FilterDefinition<TEntity> filter, FindOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindSync(filter, options, cancellationToken);

    public IAsyncCursor<TProjection> FindSync<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, FindOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindSync(session, filter, options, cancellationToken);

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(FilterDefinition<TEntity> filter, FindOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindAsync(filter, options, cancellationToken);

    public Task<IAsyncCursor<TProjection>> FindAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, FindOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindAsync(session, filter, options, cancellationToken);

    public TProjection FindOneAndDelete<TProjection>(FilterDefinition<TEntity> filter, FindOneAndDeleteOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindOneAndDelete(filter, options, cancellationToken);

    public TProjection FindOneAndDelete<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, FindOneAndDeleteOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindOneAndDelete(session, filter, options, cancellationToken);

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(FilterDefinition<TEntity> filter, FindOneAndDeleteOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindOneAndDeleteAsync(filter, options, cancellationToken);

    public Task<TProjection> FindOneAndDeleteAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, FindOneAndDeleteOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindOneAndDeleteAsync(session, filter, options, cancellationToken);

    public TProjection FindOneAndReplace<TProjection>(FilterDefinition<TEntity> filter, TEntity replacement, FindOneAndReplaceOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.FindOneAndReplace(filter, replacement, options, cancellationToken);
    }

    public TProjection FindOneAndReplace<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, TEntity replacement, FindOneAndReplaceOptions<TEntity, TProjection> options = null,
        CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.FindOneAndReplace(session, filter, replacement, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(FilterDefinition<TEntity> filter, TEntity replacement, FindOneAndReplaceOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.FindOneAndReplaceAsync(filter, replacement, options, cancellationToken);
    }

    public Task<TProjection> FindOneAndReplaceAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, TEntity replacement, FindOneAndReplaceOptions<TEntity, TProjection> options = null,
        CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.FindOneAndReplaceAsync(session, filter, replacement, options, cancellationToken);
    }

    public TProjection FindOneAndUpdate<TProjection>(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, FindOneAndUpdateOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindOneAndUpdate(filter, CheckAndSetModification(update), options, cancellationToken);

    public TProjection FindOneAndUpdate<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, FindOneAndUpdateOptions<TEntity, TProjection> options = null,
        CancellationToken cancellationToken = new())
        => inner.FindOneAndUpdate(session, filter, CheckAndSetModification(update), options, cancellationToken);

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, FindOneAndUpdateOptions<TEntity, TProjection> options = null, CancellationToken cancellationToken = new())
        => inner.FindOneAndUpdateAsync(filter, CheckAndSetModification(update), options, cancellationToken);

    public Task<TProjection> FindOneAndUpdateAsync<TProjection>(IClientSessionHandle session, FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, FindOneAndUpdateOptions<TEntity, TProjection> options = null,
        CancellationToken cancellationToken = new()) =>
        inner.FindOneAndUpdateAsync(session, filter, CheckAndSetModification(update), options, cancellationToken);

    public void InsertOne(TEntity document, InsertOneOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = document });
        inner.InsertOne(document, options, cancellationToken);
    }

    public void InsertOne(IClientSessionHandle session, TEntity document, InsertOneOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = document });
        inner.InsertOne(session, document, options, cancellationToken);
    }

    [Obsolete("Obsolete")]
    public Task InsertOneAsync(TEntity document, CancellationToken cancellationToken)
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = document });
        return inner.InsertOneAsync(document, cancellationToken);
    }

    public Task InsertOneAsync(TEntity document, InsertOneOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = document });
        return inner.InsertOneAsync(document, options, cancellationToken);
    }

    public Task InsertOneAsync(IClientSessionHandle session, TEntity document, InsertOneOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = document });
        return inner.InsertOneAsync(session, document, options, cancellationToken);
    }

    public void InsertMany(IEnumerable<TEntity> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new())
    {
        IEnumerable<TEntity> enumerable = documents.ToList();
        foreach (var doc in enumerable)
            CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = doc });

        inner.InsertMany(enumerable, options, cancellationToken);
    }

    public void InsertMany(IClientSessionHandle session, IEnumerable<TEntity> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new())
    {
        IEnumerable<TEntity> enumerable = documents.ToList();
        foreach (var doc in enumerable)
            CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = doc });

        inner.InsertMany(session, enumerable, options, cancellationToken);
    }

    public Task InsertManyAsync(IEnumerable<TEntity> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new())
    {
        IEnumerable<TEntity> enumerable = documents.ToList();
        foreach (var doc in enumerable)
            CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = doc });

        return inner.InsertManyAsync(enumerable, options, cancellationToken);
    }

    public Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TEntity> documents, InsertManyOptions options = null, CancellationToken cancellationToken = new())
    {
        IEnumerable<TEntity> enumerable = documents.ToList();
        foreach (var doc in enumerable)
            CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Added, EntryEntity = doc });

        return inner.InsertManyAsync(session, enumerable, options, cancellationToken);
    }

    [Obsolete("Obsolete")]
    public IAsyncCursor<TResult> MapReduce<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TEntity, TResult> options = null, CancellationToken cancellationToken = new())
        => inner.MapReduce(map, reduce, options, cancellationToken);

    [Obsolete("Obsolete")]
    public IAsyncCursor<TResult> MapReduce<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TEntity, TResult> options = null, CancellationToken cancellationToken = new())
        => inner.MapReduce(session, map, reduce, options, cancellationToken);

    [Obsolete("Obsolete")]
    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TEntity, TResult> options = null, CancellationToken cancellationToken = new())
        => inner.MapReduceAsync(map, reduce, options, cancellationToken);

    [Obsolete("Obsolete")]
    public Task<IAsyncCursor<TResult>> MapReduceAsync<TResult>(IClientSessionHandle session, BsonJavaScript map, BsonJavaScript reduce, MapReduceOptions<TEntity, TResult> options = null, CancellationToken cancellationToken = new())
        => inner.MapReduceAsync(session, map, reduce, options, cancellationToken);

    public IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : TEntity
        => inner.OfType<TDerivedDocument>();

    public ReplaceOneResult ReplaceOne(FilterDefinition<TEntity> filter, TEntity replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOne(filter, replacement, options, cancellationToken);
    }

    [Obsolete("Obsolete")]
    public ReplaceOneResult ReplaceOne(FilterDefinition<TEntity> filter, TEntity replacement, UpdateOptions options, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOne(filter, replacement, options, cancellationToken);
    }

    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TEntity> filter, TEntity replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOne(session, filter, replacement, options, cancellationToken);
    }

    [Obsolete("Obsolete")]
    public ReplaceOneResult ReplaceOne(IClientSessionHandle session, FilterDefinition<TEntity> filter, TEntity replacement, UpdateOptions options, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOne(session, filter, replacement, options, cancellationToken);
    }

    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TEntity> filter, TEntity replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOneAsync(filter, replacement, options, cancellationToken);
    }

    [Obsolete("Obsolete")]
    public Task<ReplaceOneResult> ReplaceOneAsync(FilterDefinition<TEntity> filter, TEntity replacement, UpdateOptions options, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOneAsync(filter, replacement, options, cancellationToken);
    }

    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, TEntity replacement, ReplaceOptions options = null, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
    }

    [Obsolete("Obsolete")]
    public Task<ReplaceOneResult> ReplaceOneAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, TEntity replacement, UpdateOptions options, CancellationToken cancellationToken = new())
    {
        CommandTrackerEventHandler?.Invoke(this, new MongoEntityEventArgs { EventState = MongoEntityEventState.Modified, EntryEntity = replacement });
        return inner.ReplaceOneAsync(session, filter, replacement, options, cancellationToken);
    }

    public UpdateResult UpdateMany(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateMany(filter, CheckAndSetModification(update), options, cancellationToken);

    public UpdateResult UpdateMany(IClientSessionHandle session, FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateMany(session, filter, CheckAndSetModification(update), options, cancellationToken);

    public Task<UpdateResult> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateManyAsync(filter, CheckAndSetModification(update), options, cancellationToken);

    public Task<UpdateResult> UpdateManyAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateManyAsync(session, filter, CheckAndSetModification(update), options, cancellationToken);

    public UpdateResult UpdateOne(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateOne(filter, CheckAndSetModification(update), options, cancellationToken);

    public UpdateResult UpdateOne(IClientSessionHandle session, FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateOne(session, filter, CheckAndSetModification(update), options, cancellationToken);

    public Task<UpdateResult> UpdateOneAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateOneAsync(filter, CheckAndSetModification(update), options, cancellationToken);

    public Task<UpdateResult> UpdateOneAsync(IClientSessionHandle session, FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, UpdateOptions options = null, CancellationToken cancellationToken = new())
        => inner.UpdateOneAsync(session, filter, CheckAndSetModification(update), options, cancellationToken);

    public IChangeStreamCursor<TResult> Watch<TResult>(PipelineDefinition<ChangeStreamDocument<TEntity>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new())
        => inner.Watch(pipeline, options, cancellationToken);

    public IChangeStreamCursor<TResult> Watch<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TEntity>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new())
        => inner.Watch(session, pipeline, options, cancellationToken);

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(PipelineDefinition<ChangeStreamDocument<TEntity>, TResult> pipeline, ChangeStreamOptions options = null, CancellationToken cancellationToken = new())
        => inner.WatchAsync(pipeline, options, cancellationToken);

    public Task<IChangeStreamCursor<TResult>> WatchAsync<TResult>(IClientSessionHandle session, PipelineDefinition<ChangeStreamDocument<TEntity>, TResult> pipeline, ChangeStreamOptions options = null,
        CancellationToken cancellationToken = new())
        => inner.WatchAsync(session, pipeline, options, cancellationToken);

    public IMongoCollection<TEntity> WithReadConcern(ReadConcern readConcern)
        => new TrackingMongoCollection<TEntity>(inner.WithReadConcern(readConcern), commandTrackerEventHandler);

    public IMongoCollection<TEntity> WithReadPreference(ReadPreference readPreference)

        => new TrackingMongoCollection<TEntity>(inner.WithReadPreference(readPreference), commandTrackerEventHandler);

    public IMongoCollection<TEntity> WithWriteConcern(WriteConcern writeConcern)
        => new TrackingMongoCollection<TEntity>(inner.WithWriteConcern(writeConcern), commandTrackerEventHandler);

    public CollectionNamespace CollectionNamespace => inner.CollectionNamespace;

    public IMongoDatabase Database => inner.Database;

    public IBsonSerializer<TEntity> DocumentSerializer => inner.DocumentSerializer;

    public IMongoIndexManager<TEntity> Indexes => inner.Indexes;

    public IMongoSearchIndexManager SearchIndexes => inner.SearchIndexes;

    public MongoCollectionSettings Settings => inner.Settings;
}