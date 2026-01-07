using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace HsnSoft.Base.Domain.Repositories;

public interface IEfCoreGenericRepository<TEntity, in TKey> : IGenericRepository<TEntity, TKey>, IEfCoreBulkRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    DbSet<TEntity> GetDbSet();
    IQueryable<TEntity> GetQueryable();

    // IQueryable<TEntity> WithDetails();
    //
    // IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors);

    // Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> UpdateByExpressionAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken cancellationToken = default);

    #region Raw SQL

    Task<int> ExecuteSqlAsync(string sql, [ItemCanBeNull] [CanBeNull] object[] parameters = null, CancellationToken cancellationToken = default);

    IQueryable<TEntity> FromSql(string sql, params object[] parameters);

    #endregion
}