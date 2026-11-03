using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using Microsoft.Extensions.DependencyInjection;
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
        => await Collection.Find(Concepts.Visible(Builders<TEntity>.Filter.Eq(IdField, id))).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await Collection.Find(Concepts.Visible(Builders<TEntity>.Filter.Where(predicate))).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TDto?> FindAsync<TDto>(TKey id, Expression<Func<TEntity, TDto>>? selector = null, CancellationToken cancellationToken = default)
        where TDto : class
        => await Collection.Find(Concepts.Visible(Builders<TEntity>.Filter.Eq(IdField, id))).Project(selector ?? EntityProjection.For<TEntity, TDto>()).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, bool ignoreFilters = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        FilterDefinition<TEntity> filter = Builders<TEntity>.Filter.Where(predicate);
        return Collection.Find(ignoreFilters ? filter : Concepts.Visible(filter)).AnyAsync(cancellationToken);
    }

    public Task<long> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
        => Collection.CountDocumentsAsync(Concepts.Visible(Narrowed(filter)), cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetListAsync(Domain.Repositories.ListQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        IFindFluent<TEntity, TEntity> find = Collection.Find(Concepts.Visible(Narrowed(options.Filter))).Sort(Sorting(options));
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

        IFindFluent<TEntity, TDto> find = Collection.Find(Concepts.Visible(Narrowed(options.Filter))).Sort(Sorting(options)).Project(selector ?? EntityProjection.For<TEntity, TDto>());
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
        long total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        List<TDto> items = await Collection.Find(filter).Sort(Sorting(options)).Skip((Math.Max(options.PageNumber, 1) - 1) * pageSize).Limit(pageSize).Project(selector ?? EntityProjection.For<TEntity, TDto>()).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TDto>(items, total);
    }

    public async Task<PagedQueryResult<TEntity>> GetPagedListAsync(PagedQueryOptions<TEntity> options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        int pageSize = Math.Max(options.MaxResultCount ?? PagedQueryOptions<TEntity>.DefaultPageSize, 1);
        FilterDefinition<TEntity> filter = Concepts.Visible(Narrowed(options.Filter));
        long total = await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        List<TEntity> items = await Collection.Find(filter).Sort(Sorting(options)).Skip((Math.Max(options.PageNumber, 1) - 1) * pageSize).Limit(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedQueryResult<TEntity>(items, total);
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Concepts.Created(entity);
        return Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // an entity with a stamp is replaced only where the stored document still has the stamp its caller saw
        string? seen = Concepts.Modified(entity);
        ReplaceOneResult result = await Collection.ReplaceOneAsync(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen), entity, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.MatchedCount == 0)
        {
            throw ConcurrencyConflict.For(typeof(TEntity).Name);
        }
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // an entity that can be soft deleted is marked and replaced; any other is removed; an entity with a stamp is only where the document still has the stamp its caller saw
        if (Concepts.SoftDeleted(entity))
        {
            string? seen = Concepts.Modified(entity);
            ReplaceOneResult replaced = await Collection.ReplaceOneAsync(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen), entity, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (replaced.MatchedCount == 0)
            {
                throw ConcurrencyConflict.For(typeof(TEntity).Name);
            }

            return;
        }

        string? stamp = entity is IHasConcurrencyStamp { ConcurrencyStamp: { Length: > 0 } carried } ? carried : null;
        DeleteResult deleted = await Collection.DeleteOneAsync(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, stamp), cancellationToken).ConfigureAwait(false);
        if (deleted.DeletedCount == 0)
        {
            throw ConcurrencyConflict.For(typeof(TEntity).Name);
        }
    }

    public async Task AddManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<TEntity> list = [.. entities];
        foreach (TEntity entity in list)
        {
            Concepts.Created(entity);
        }

        if (list.Count > 0)
        {
            await Collection.InsertManyAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        List<WriteModel<TEntity>> models = [];
        foreach (TEntity entity in entities)
        {
            string? seen = Concepts.Modified(entity);
            models.Add(new ReplaceOneModel<TEntity>(MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen), entity));
        }

        if (models.Count > 0)
        {
            BulkWriteResult<TEntity> result = await Collection.BulkWriteAsync(models, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.MatchedCount < models.Count)
            {
                throw ConcurrencyConflict.For(typeof(TEntity).Name);
            }
        }
    }

    public async Task DeleteManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        // an entity that can be soft deleted is marked and replaced; any other is removed
        List<WriteModel<TEntity>> models = [];
        foreach (TEntity entity in entities)
        {
            string? seen = Concepts.SoftDeleted(entity) ? Concepts.Modified(entity) : entity is IHasConcurrencyStamp { ConcurrencyStamp: { Length: > 0 } carried } ? carried : null;
            FilterDefinition<TEntity> matching = MongoEntityConcepts.Matching<TEntity>(IdField, entity.Id!, seen);
            models.Add(entity is ISoftDelete ? new ReplaceOneModel<TEntity>(matching, entity) : new DeleteOneModel<TEntity>(matching));
        }

        if (models.Count > 0)
        {
            BulkWriteResult<TEntity> result = await Collection.BulkWriteAsync(models, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.MatchedCount + result.DeletedCount < models.Count)
            {
                throw ConcurrencyConflict.For(typeof(TEntity).Name);
            }
        }
    }

    public async Task<long> DeleteWhereAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        FilterDefinition<TEntity> matched = Concepts.Visible(Builders<TEntity>.Filter.Where(filter));
        if (!typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            return (await Collection.DeleteManyAsync(matched, cancellationToken).ConfigureAwait(false)).DeletedCount;
        }

        // nothing is loaded here: the marks that SoftDeleted writes on an entity are written by the update
        return (await Collection.UpdateManyAsync(matched, Concepts.Deletion<TEntity>(), cancellationToken: cancellationToken).ConfigureAwait(false)).ModifiedCount;
    }

    public async Task<long> UpdateWhereAsync(Expression<Func<TEntity, bool>> filter, Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(update);

        FilterDefinition<TEntity> matched = Concepts.Visible(Builders<TEntity>.Filter.Where(filter));
        return (await Collection.UpdateManyAsync(matched, Concepts.Modification(update(Builders<TEntity>.Update)), cancellationToken: cancellationToken).ConfigureAwait(false)).MatchedCount;
    }

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
