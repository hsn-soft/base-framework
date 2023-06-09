using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;

namespace HsnSoft.Base.Domain.Repositories;

public interface IReadOnlyRepository<TEntity> : IReadOnlyBasicRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<IQueryable<TEntity>> WithDetailsAsync(); //TODO: CancellationToken

    Task<IQueryable<TEntity>> WithDetailsAsync(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    Task<IQueryable<TEntity>> GetQueryableAsync(); //TODO: CancellationToken
}

public interface IReadOnlyRepository<TEntity, in TKey> : IReadOnlyRepository<TEntity>, IReadOnlyBasicRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
}
