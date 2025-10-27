using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace HsnSoft.Base.Domain.Repositories;

public interface IEfCoreBulkRepository<in TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
{
    // INSERT
    Task BulkInsertAsync(
        IEnumerable<TEntity> entities,
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);

    // UPDATE
    Task BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);

    // DELETE
    Task BulkDeleteAsync(
        IEnumerable<TEntity> entities,
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);

    // MERGE (UPSERT)
    Task BulkMergeAsync(
        IEnumerable<TEntity> entities,
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);

    // SYNC (Insert or Update or Delete)
    Task BulkSyncAsync(
        IEnumerable<TEntity> entities,
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);

    // READ
    Task BulkReadAsync(
        IEnumerable<TEntity> entities,
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);

    // TRUNCATE
    Task BulkTruncateAsync(
        [CanBeNull] Action<BulkConfig> configAction = null,
        CancellationToken cancellationToken = default);
}