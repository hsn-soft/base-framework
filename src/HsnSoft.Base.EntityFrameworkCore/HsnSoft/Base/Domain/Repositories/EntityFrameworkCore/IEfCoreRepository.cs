using HsnSoft.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IEfCoreRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    DbContext GetDbContext();

    DbSet<TEntity> GetDbSet();
}

public interface IEfCoreRepository<TEntity, TKey> : IEfCoreRepository<TEntity>, IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
}