using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MongoDB.Context;
using MongoDB.Driver;

namespace HsnSoft.Base.Domain.Repositories;

public interface IMongoGenericRepository<TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    ITrackingMongoCollection<TEntity> GetCollection();
    IQueryable<TEntity> GetQueryable();

    Task<long> UpdateByExpressionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> set,
        CancellationToken cancellationToken = default);
}