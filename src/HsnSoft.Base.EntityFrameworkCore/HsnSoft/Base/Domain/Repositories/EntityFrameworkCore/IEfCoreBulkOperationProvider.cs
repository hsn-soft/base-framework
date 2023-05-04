using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Domain.Repositories.EntityFrameworkCore;

public interface IEfCoreBulkOperationProvider
{
    Task InsertManyAsync<TDbContext, TEntity>(
        IEfCoreRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        bool autoSave,
        CancellationToken cancellationToken
    )
        where TDbContext : DbContext
        where TEntity : class, IEntity;


    Task UpdateManyAsync<TDbContext, TEntity>(
        IEfCoreRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        bool autoSave,
        CancellationToken cancellationToken
    )
        where TDbContext : DbContext
        where TEntity : class, IEntity;


    Task DeleteManyAsync<TDbContext, TEntity>(
        IEfCoreRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        bool autoSave,
        CancellationToken cancellationToken
    )
        where TDbContext : DbContext
        where TEntity : class, IEntity;
}