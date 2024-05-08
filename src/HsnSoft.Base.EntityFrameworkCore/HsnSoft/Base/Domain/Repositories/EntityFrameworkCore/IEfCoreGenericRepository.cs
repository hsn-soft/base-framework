using System;
using System.Linq;
using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IEfCoreGenericRepository<out TDbContext, TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    TDbContext GetDbContext();

    DbSet<TEntity> GetDbSet();

    IQueryable<TEntity> WithDetails(); //TODO: CancellationToken

    IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors); //TODO: CancellationToken

    IQueryable<TEntity> GetQueryable(); //TODO: CancellationToken
}