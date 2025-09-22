using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories;

public interface IEfCoreGenericRepository<TEntity, in TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    DbSet<TEntity> GetDbSet();
    IQueryable<TEntity> GetQueryable();

    // IQueryable<TEntity> WithDetails();
    //
    // IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}