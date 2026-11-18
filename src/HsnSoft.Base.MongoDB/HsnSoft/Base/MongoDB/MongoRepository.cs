using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// <see cref="IRepository{TEntity, TKey}"/> for a collection of MongoDB: the queries of a service's entity, written once. The filters of the concepts (soft delete, tenants: <see cref="MongoEntityConcepts"/>) apply to every method
/// but <c>AnyAsync(..., ignoreFilters: true)</c>, and the audit fields and the tenant are written before a write. The entity needs a class map (the domain has no attribute of the driver) that maps <c>Id</c> as the id.
/// A service that needs more derives from it and adds its own methods.
/// </summary>
public class MongoRepository<TEntity, TKey>(IMongoDatabase database, MongoEntityConcepts concepts, string collectionName) : IMongoRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private const string IdField = nameof(IEntity<>.Id);

    protected IMongoCollection<TEntity> Collection { get; } = database.GetCollection<TEntity>(collectionName);

    protected MongoEntityConcepts Concepts { get; } = concepts;

    public async Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default)
        => Followed(await Find(Concepts.Visible(Builders<TEntity>.Filter.Eq(IdField, id))).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false));

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Followed(await Find(Concepts.Visible(Builders<TEntity>.Filter.Where(predicate))).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task<TDto?> FindAsync<TDto>(TKey id, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
        => await Find(Concepts.Visible(Builders<TEntity>.Filter.Eq(IdField, id))).Project(selector ?? EntityProjection.For<TEntity, TDto>()).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreFilters = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        FilterDefinition<TEntity> filter = Builders<TEntity>.Filter.Where(predicate);
        return Find(ignoreFilters ? filter : Concepts.Visible(filter)).AnyAsync(cancellationToken);
    }

    public Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
        => CountDocuments(Concepts.Visible(Narrowed(filter)), cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetListAsync(Domain.Repositories.ListQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        IFindFluent<TEntity, TEntity> find = Find(Concepts.Visible(Narrowed(options.Filter))).Sort(Sorting(options));
        if (options.MaxResultCount is { } limit)
        {
            find = find.Limit(limit);
        }

        return await find.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TDto>> GetListAsync<TDto>(Domain.Repositories.ListQueryOptions<TEntity> options, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(options);

        IFindFluent<TEntity, TDto> find = Find(Concepts.Visible(Narrowed(options.Filter))).Sort(Sorting(options)).Project(selector ?? EntityProjection.For<TEntity, TDto>());
        if (options.MaxResultCount is { } limit)
        {
            find = find.Limit(limit);
        }

        return await find.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedQueryResult<TDto>> GetPagedListAsync<TDto>(PagedQueryOptions<TEntity> options, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
    {
        ArgumentNullException.ThrowIfNull(options);

        int pageSize = Math.Max(options.MaxResultCount ?? PagedQueryOptions<TEntity>.DefaultPageSize, 1);
        FilterDefinition<TEntity> filter = Concepts.Visible(Narrowed(options.Filter));
        long total = await CountDocuments(filter, cancellationToken).ConfigureAwait(false);
        List<TDto> items = await Find(filter).Sort(Sorting(options)).Skip((Math.Max(options.PageNumber, 1) - 1) * pageSize).Limit(pageSize).Project(selector ?? EntityProjection.For<TEntity, TDto>()).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TDto>(items, total);
    }

    public async Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        int pageSize = Math.Max(options.MaxResultCount ?? PagedQueryOptions<TEntity>.DefaultPageSize, 1);
        FilterDefinition<TEntity> filter = Concepts.Visible(Narrowed(options.Filter));
        long total = await CountDocuments(filter, cancellationToken).ConfigureAwait(false);
        List<TEntity> items = await Find(filter).Sort(Sorting(options)).Skip((Math.Max(options.PageNumber, 1) - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TEntity>(items, total);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        EntityKeys.EnsureKey(entity);
        Concepts.Created(entity);
        await InsertOne(entity, cancellationToken).ConfigureAwait(false);
        Remember(entity);
        return entity;   // the same instance: the key, the audit fields and the stamp were written before the insert, so the caller needs no second read
    }

    public async Task<WriteOutcome> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // an entity that FindAsync returned is followed: when nothing in it differs from what was read, nothing is written (the stamp and the audit fields stay)
        if (Unchanged(entity))
        {
            return WriteOutcome.NotModified;
        }

        // an entity with a stamp is replaced only where the stored document still has the stamp its caller saw
        string? seen = Concepts.Modified(entity);
        ReplaceOneResult result = await ReplaceOne(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen), entity, cancellationToken).ConfigureAwait(false);
        if (result.MatchedCount == 0)
        {
            throw await FailureOfAsync([(entity.Id!, seen, StampOf(entity))], cancellationToken).ConfigureAwait(false);
        }

        Remember(entity);
        return WriteOutcome.Modified;
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // an entity that can be soft deleted is marked and replaced; any other is removed; an entity with a stamp is only where the document still has the stamp its caller saw
        if (Concepts.SoftDeleted(entity))
        {
            string? seen = Concepts.Modified(entity);
            ReplaceOneResult replaced = await ReplaceOne(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen), entity, cancellationToken).ConfigureAwait(false);
            if (replaced.MatchedCount == 0)
            {
                throw await FailureOfAsync([(entity.Id!, seen, StampOf(entity))], cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        string? stamp = entity is IHasConcurrencyStamp { ConcurrencyStamp: { Length: > 0 } carried } ? carried : null;
        DeleteResult deleted = await DeleteOne(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, stamp), cancellationToken).ConfigureAwait(false);
        if (deleted.DeletedCount == 0)
        {
            throw await FailureOfAsync([(entity.Id!, stamp, stamp)], cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<TEntity> list = [.. entities];
        foreach (TEntity entity in list)
        {
            EntityKeys.EnsureKey(entity);
            Concepts.Created(entity);
        }

        if (list.Count == 0)
        {
            return;
        }

        try
        {
            await InsertMany(list, cancellationToken).ConfigureAwait(false);
        }
        catch (MongoBulkWriteException<TEntity> exception)
        {
            // no transaction: the documents are inserted in order and stop at the one that failed. All or none: the ones that were inserted are removed (best effort; a transaction guarantees it)
            if (MongoTransactionScope.Current is null && exception.Result.InsertedCount > 0)
            {
                await RemoveInsertedAsync(list.Take((int)exception.Result.InsertedCount), cancellationToken).ConfigureAwait(false);
            }

            throw DuplicateOf(exception) ?? (Exception)exception;
        }

        list.ForEach(Remember);
    }

    public async Task<WriteResult> UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<WriteModel<TEntity>> models = [];
        List<(object Id, string? Seen, string? Now)> written = [];
        List<TEntity> changed = [];
        int requested = 0;
        foreach (TEntity entity in entities)
        {
            requested++;
            if (Unchanged(entity))
            {
                continue;
            }

            string? seen = Concepts.Modified(entity);
            models.Add(new ReplaceOneModel<TEntity>(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen), entity));
            written.Add((entity.Id!, seen, StampOf(entity)));
            changed.Add(entity);
        }

        if (models.Count > 0)
        {
            BulkWriteResult<TEntity> result = await BulkWrite(models, cancellationToken).ConfigureAwait(false);
            if (result.MatchedCount < models.Count)
            {
                // each document is written on its own: without a transaction the others were written, and the exception says which were not
                throw await FailureOfAsync(written, cancellationToken).ConfigureAwait(false);
            }

            changed.ForEach(Remember);
        }

        return new WriteResult(requested, models.Count);
    }

    public async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        // an entity that can be soft deleted is marked and replaced; any other is removed
        List<WriteModel<TEntity>> models = [];
        List<(object Id, string? Seen, string? Now)> targets = [];
        foreach (TEntity entity in entities)
        {
            string? seen = Concepts.SoftDeleted(entity) ? Concepts.Modified(entity) : entity is IHasConcurrencyStamp { ConcurrencyStamp: { Length: > 0 } carried } ? carried : null;
            FilterDefinition<TEntity> matching = MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen);
            models.Add(entity is ISoftDelete ? new ReplaceOneModel<TEntity>(matching, entity) : new DeleteOneModel<TEntity>(matching));
            targets.Add((entity.Id!, seen, StampOf(entity)));
        }

        if (models.Count > 0)
        {
            // a document that is removed for good leaves nothing to ask afterwards, so a batch that removes any is checked first (one read of the keys): the failure names exactly the records that are not there or not current, and nothing is removed
            if (models.Any(model => model is DeleteOneModel<TEntity>))
            {
                if (await FindProblemsAsync(targets, cancellationToken).ConfigureAwait(false) is { } problem)
                {
                    throw problem;
                }
            }

            BulkWriteResult<TEntity> result = await BulkWrite(models, cancellationToken).ConfigureAwait(false);
            if (result.MatchedCount + result.DeletedCount < models.Count)
            {
                throw await FailureOfAsync(targets, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task AddManyInTransactionAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<TEntity> list = [.. entities];
        return MongoTransactions.RunAsync<bool>(database.Client, async token =>
        {
            await AddManyAsync(list, token).ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }

    public Task<WriteResult> UpdateManyInTransactionAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<TEntity> list = [.. entities];
        return MongoTransactions.RunAsync(database.Client, token => UpdateManyAsync(list, token), cancellationToken);
    }

    public Task DeleteManyInTransactionAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<TEntity> list = [.. entities];
        return MongoTransactions.RunAsync<bool>(database.Client, async token =>
        {
            await DeleteManyAsync(list, token).ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }

    public async Task<long> DeleteWhereAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        FilterDefinition<TEntity> matched = Concepts.Visible(Builders<TEntity>.Filter.Where(filter));
        if (!typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            return (await DeleteMany(matched, cancellationToken).ConfigureAwait(false)).DeletedCount;
        }

        // nothing is loaded here: the marks that SoftDeleted writes on an entity are written by the update
        return (await UpdateMany(matched, Concepts.Deletion<TEntity>(), cancellationToken).ConfigureAwait(false)).ModifiedCount;
    }

    public async Task<long> UpdateWhereAsync(Expression<Func<TEntity, bool>> filter, Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(update);

        FilterDefinition<TEntity> matched = Concepts.Visible(Builders<TEntity>.Filter.Where(filter));
        return (await UpdateMany(matched, Concepts.Modification(update(Builders<TEntity>.Update)), cancellationToken).ConfigureAwait(false)).MatchedCount;
    }

    // ---- the session of the transaction a unit of work has open: every call below goes through it (outside one, there is none) ----
    private static IClientSessionHandle? Session => MongoTransactionScope.Current;

    private IFindFluent<TEntity, TEntity> Find(FilterDefinition<TEntity> filter) => Session is { } session ? Collection.Find(session, filter) : Collection.Find(filter);

    private Task<long> CountDocuments(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
        => Session is { } session ? Collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken) : Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

    private Task InsertOne(TEntity entity, CancellationToken cancellationToken)
        => Guard(() => Session is { } session ? Collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken) : Collection.InsertOneAsync(entity, cancellationToken: cancellationToken));

    private Task InsertMany(List<TEntity> list, CancellationToken cancellationToken)
        => Session is { } session ? Collection.InsertManyAsync(session, list, cancellationToken: cancellationToken) : Collection.InsertManyAsync(list, cancellationToken: cancellationToken);

    private Task<ReplaceOneResult> ReplaceOne(FilterDefinition<TEntity> filter, TEntity entity, CancellationToken cancellationToken)
        => Guard(() => Session is { } session ? Collection.ReplaceOneAsync(session, filter, entity, cancellationToken: cancellationToken) : Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken));

    private Task<DeleteResult> DeleteOne(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
        => Session is { } session ? Collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken) : Collection.DeleteOneAsync(filter, cancellationToken);

    private Task<DeleteResult> DeleteMany(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
        => Session is { } session ? Collection.DeleteManyAsync(session, filter, cancellationToken: cancellationToken) : Collection.DeleteManyAsync(filter, cancellationToken);

    private Task<UpdateResult> UpdateMany(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken)
        => Guard(() => Session is { } session ? Collection.UpdateManyAsync(session, filter, update, cancellationToken: cancellationToken) : Collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken));

    private Task<BulkWriteResult<TEntity>> BulkWrite(List<WriteModel<TEntity>> models, CancellationToken cancellationToken)
        => Guard(() => Session is { } session ? Collection.BulkWriteAsync(session, models, cancellationToken: cancellationToken) : Collection.BulkWriteAsync(models, cancellationToken: cancellationToken));

    // a write that would break a unique index is a RepositoryException (DuplicateKey), whichever call it was
    private static async Task<TResult> Guard<TResult>(Func<Task<TResult>> write)
    {
        try
        {
            return await write().ConfigureAwait(false);
        }
        catch (Exception exception) when (DuplicateOf(exception) is not null)
        {
            throw DuplicateOf(exception)!;
        }
    }

    private static async Task Guard(Func<Task> write)
    {
        await Guard<bool>(async () =>
        {
            await write().ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    private static readonly Regex s_indexName = new(@"index: (\S+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static RepositoryException? DuplicateOf(Exception exception)
    {
        string? message = exception switch
        {
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } write => write.WriteError.Message,
            MongoBulkWriteException bulk when bulk.WriteErrors.Any(error => error.Category == ServerErrorCategory.DuplicateKey) => bulk.WriteErrors.First(error => error.Category == ServerErrorCategory.DuplicateKey).Message,
            _ => null,
        };
        if (message is null)
        {
            return null;
        }

        Match match = s_indexName.Match(message);
        return RepositoryException.Duplicate(typeof(TEntity).Name, match.Success ? match.Groups[1].Value : null, exception);
    }

    private async Task RemoveInsertedAsync(IEnumerable<TEntity> inserted, CancellationToken cancellationToken)
    {
        List<object> ids = [.. inserted.Select(entity => (object)entity.Id!)];
        if (ids.Count > 0)
        {
            await DeleteMany(Builders<TEntity>.Filter.In(IdField, ids), cancellationToken).ConfigureAwait(false);
        }
    }

    // ---- what a write that matched nothing says: which records are not there, and which are there but not current (one read, only on this path) ----
    private async Task<RepositoryException> FailureOfAsync(IReadOnlyList<(object Id, string? Seen, string? Now)> targets, CancellationToken cancellationToken)
        => await FindProblemsAsync(targets, cancellationToken).ConfigureAwait(false) ?? RepositoryException.Conflict(typeof(TEntity).Name, targets.Select(target => target.Id));

    // the records that are not there (a soft deleted one is not there), else the records that are there and are neither the stamp the caller saw nor the one this write gave: null when there is none (a write that raced)
    private async Task<RepositoryException?> FindProblemsAsync(IReadOnlyList<(object Id, string? Seen, string? Now)> targets, CancellationToken cancellationToken)
    {
        string entityName = typeof(TEntity).Name;
        List<BsonDocument> stored = await Find(Builders<TEntity>.Filter.In(IdField, targets.Select(target => target.Id))).Project<BsonDocument>(Builders<TEntity>.Projection.Include(IdField).Include(nameof(IHasConcurrencyStamp.ConcurrencyStamp)).Include(nameof(ISoftDelete.IsDeleted))).ToListAsync(cancellationToken).ConfigureAwait(false);

        // a document that was soft deleted is gone to the caller, as one that was removed
        Dictionary<string, string?> found = stored
            .Where(document => !(document.TryGetValue(nameof(ISoftDelete.IsDeleted), out BsonValue deleted) && deleted.IsBoolean && deleted.AsBoolean))
            .ToDictionary(document => Text(document["_id"]), document => document.TryGetValue(nameof(IHasConcurrencyStamp.ConcurrencyStamp), out BsonValue value) && value.IsString ? value.AsString : null, StringComparer.Ordinal);

        List<object> missing = [.. targets.Where(target => !found.ContainsKey(Text(target.Id))).Select(target => target.Id)];
        if (missing.Count > 0)
        {
            return RepositoryException.NotFound(entityName, missing);
        }

        List<object> notCurrent = [.. targets.Where(target => target.Seen is not null && found[Text(target.Id)] != target.Seen && found[Text(target.Id)] != target.Now).Select(target => target.Id)];
        return notCurrent.Count > 0 ? RepositoryException.Conflict(entityName, notCurrent) : null;
    }

    private static string? StampOf(TEntity entity) => entity is IHasConcurrencyStamp { ConcurrencyStamp: { Length: > 0 } stamp } ? stamp : null;

    private static string Text(object id) => id is BsonValue value ? value.ToString()! : Convert.ToString(id, System.Globalization.CultureInfo.InvariantCulture)!;

    private static string Text(BsonValue id) => id.IsBsonBinaryData && id.AsBsonBinaryData.SubType is BsonBinarySubType.UuidStandard or BsonBinarySubType.UuidLegacy ? id.AsGuid.ToString() : id.ToString()!;

    // ---- the entities FindAsync returned are followed: a copy of what was read, to tell a change from none ----
    private readonly ConditionalWeakTable<TEntity, BsonDocument> _snapshots = [];

    private TEntity? Followed(TEntity? entity)
    {
        if (entity is not null)
        {
            Remember(entity);
        }

        return entity;
    }

    private void Remember(TEntity entity)
    {
        _snapshots.Remove(entity);
        _snapshots.Add(entity, entity.ToBsonDocument());
    }

    private bool Unchanged(TEntity entity) => _snapshots.TryGetValue(entity, out BsonDocument? read) && read.Equals(entity.ToBsonDocument());

    private static FilterDefinition<TEntity> Narrowed(Expression<Func<TEntity, bool>>? filter)
        => filter is null ? Builders<TEntity>.Filter.Empty : Builders<TEntity>.Filter.Where(filter);

    // a page needs an order to be the same page twice: without one the key orders it
    private static SortDefinition<TEntity> Sorting(Domain.Repositories.ListQueryOptions<TEntity> options)
    {
        SortVisitor visitor = new();
        if (options.Sort is { Clauses.Count: > 0 })
        {
            options.Sort.Accept(visitor);
        }
        else
        {
            visitor.Sorts.Add(Builders<TEntity>.Sort.Ascending(IdField));
        }

        return Builders<TEntity>.Sort.Combine(visitor.Sorts);
    }

    private sealed class SortVisitor : ISortVisitor<TEntity>
    {
        public List<SortDefinition<TEntity>> Sorts { get; } = [];

        public void Visit<TSortKey>(Expression<Func<TEntity, TSortKey>> key, bool descending)
        {
            ExpressionFieldDefinition<TEntity, TSortKey> field = new(key);
            Sorts.Add(descending ? Builders<TEntity>.Sort.Descending(field) : Builders<TEntity>.Sort.Ascending(field));
        }
    }
}

public static class MongoRepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MongoRepository{TEntity, TKey}"/> over the collection <paramref name="collectionName"/> as the <see cref="IRepository{TEntity, TKey}"/> (and <see cref="IMongoRepository{TEntity, TKey}"/>) of an entity (after <c>AddBaseMongo</c>), and registers the maps of the base classes
    /// of the entities (<see cref="MongoEntityMaps"/>). <paramref name="serviceKey"/> is the key of the <c>AddBaseMongo</c> that registered the database the collection is in, when the service has more than one.
    /// </summary>
    public static IServiceCollection AddMongoRepository<TEntity, TKey>(this IServiceCollection services, string collectionName, string? serviceKey = null)
        where TEntity : class, IEntity<TKey>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        MongoEntityMaps.RegisterBases<TKey>();
        services.AddScoped<IMongoRepository<TEntity, TKey>>(provider => new MongoRepository<TEntity, TKey>(
            serviceKey is null ? provider.GetRequiredService<IMongoDatabase>() : provider.GetRequiredKeyedService<IMongoDatabase>(serviceKey), provider.GetRequiredService<MongoEntityConcepts>(), collectionName));
        return services.AddScoped<IRepository<TEntity, TKey>>(provider => provider.GetRequiredService<IMongoRepository<TEntity, TKey>>());
    }
}
