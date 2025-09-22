using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Repositories;

public interface IGenericRepository<TEntity, in TKey> : IReadOnlyGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    Task<int> InsertAsync([NotNull] TEntity entity, CancellationToken cancellationToken = default);
    Task<int> InsertManyAsync([NotNull] IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateByIdAsync(TKey id, Action<TEntity> updateAction, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync([NotNull] TEntity entity, CancellationToken cancellationToken = default);
    Task<int> UpdateManyAsync([NotNull] IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<int> DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<int> DeleteByIdListAsync([NotNull] IEnumerable<TKey> ids, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync([NotNull] TEntity entity, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync([NotNull] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync([NotNull] IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
}