using System;
using System.Collections.Generic;
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

    IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default);
    Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default);

    Task<TEntity> GetByIdAsync(IClientSessionHandle session, TKey id, CancellationToken cancellationToken = default);

    Task InsertAsync(IClientSessionHandle session, TEntity entity, CancellationToken cancellationToken = default);
    Task InsertManyAsync(IClientSessionHandle session, IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task UpdateAsync(IClientSessionHandle session,TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateManyAsync(IClientSessionHandle session, IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<long> UpdateByExpressionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> set,
        CancellationToken cancellationToken = default);

    Task<long> UpdateByExpressionAsync(
        IClientSessionHandle session,
        Expression<Func<TEntity, bool>> predicate,
        Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> set,
        CancellationToken cancellationToken = default);
}