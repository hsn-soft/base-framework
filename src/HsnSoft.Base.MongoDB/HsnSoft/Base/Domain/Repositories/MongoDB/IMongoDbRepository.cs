using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HsnSoft.Base.Domain.Repositories.MongoDB;

public interface IMongoDbRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    [Obsolete("Use GetDatabaseAsync method.")]
    IMongoDatabase Database { get; }
    

    [Obsolete("Use GetCollectionAsync method.")]
    IMongoCollection<TEntity> Collection { get; }
    

    [Obsolete("Use GetMongoQueryableAsync method.")]
    IMongoQueryable<TEntity> GetMongoQueryable();

    Task<IMongoQueryable<TEntity>> GetMongoQueryableAsync(CancellationToken cancellationToken = default, AggregateOptions options = null);

    Task<IAggregateFluent<TEntity>> GetAggregateAsync(CancellationToken cancellationToken = default, AggregateOptions options = null);
}

public interface IMongoDbRepository<TEntity, TKey> : IMongoDbRepository<TEntity>, IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
}