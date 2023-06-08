using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories.MongoDB;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HsnSoft.Base.Domain.Repositories;

public static class MongoDbCoreRepositoryExtensions
{
    public static Task<IMongoDatabase> GetDatabaseAsync<TEntity>(this IReadOnlyBasicRepository<TEntity> repository, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity
    {
        return repository.ToMongoDbRepository().GetDatabaseAsync(cancellationToken);
    }

    public static Task<IMongoCollection<TEntity>> GetCollectionAsync<TEntity>(this IReadOnlyBasicRepository<TEntity> repository, CancellationToken cancellationToken = default)
        where TEntity : class, IEntity
    {
        return repository.ToMongoDbRepository().GetCollectionAsync(cancellationToken);
    }

    public static Task<IMongoQueryable<TEntity>> GetMongoQueryableAsync<TEntity>(this IReadOnlyBasicRepository<TEntity> repository, CancellationToken cancellationToken = default, AggregateOptions aggregateOptions = null)
        where TEntity : class, IEntity
    {
        return repository.ToMongoDbRepository().GetMongoQueryableAsync(cancellationToken, aggregateOptions);
    }

    public static Task<IAggregateFluent<TEntity>> GetAggregateAsync<TEntity>(this IReadOnlyBasicRepository<TEntity> repository, CancellationToken cancellationToken = default, AggregateOptions aggregateOptions = null)
        where TEntity : class, IEntity
    {
        return repository.ToMongoDbRepository().GetAggregateAsync(cancellationToken, aggregateOptions);
    }

    public static IMongoDbRepository<TEntity> ToMongoDbRepository<TEntity>(this IReadOnlyBasicRepository<TEntity> repository)
        where TEntity : class, IEntity
    {
        if (repository is IMongoDbRepository<TEntity> mongoDbRepository)
        {
            return mongoDbRepository;
        }

        throw new ArgumentException("Given repository does not implement " + typeof(IMongoDbRepository<TEntity>).AssemblyQualifiedName, nameof(repository));
    }
}