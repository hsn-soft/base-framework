using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using MongoDB.Driver;

namespace HsnSoft.Base.Domain.Repositories.MongoDB;

public interface IMongoDbBulkOperationProvider
{
    Task InsertManyAsync<TEntity>(
        IMongoDbRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        IClientSessionHandle sessionHandle,
        bool autoSave,
        CancellationToken cancellationToken
    )
        where TEntity : class, IEntity;

    Task UpdateManyAsync<TEntity>(
        IMongoDbRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        IClientSessionHandle sessionHandle,
        bool autoSave,
        CancellationToken cancellationToken
    )
        where TEntity : class, IEntity;

    Task DeleteManyAsync<TEntity>(
        IMongoDbRepository<TEntity> repository,
        IEnumerable<TEntity> entities,
        IClientSessionHandle sessionHandle,
        bool autoSave,
        CancellationToken cancellationToken
    )
        where TEntity : class, IEntity;
}