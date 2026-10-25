using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>What only a MongoDB store can do, beside what every store does (<see cref="IRepository{TEntity, TKey}"/>).</summary>
public interface IMongoRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// Applies <paramref name="update"/> (<c>set =&gt; set.Inc(product =&gt; product.Stock, -3)</c>) to every document the filter matches, in one statement and without loading them, and says how many there were. The check of the filter and the change are one step in the
    /// database, so two callers at once cannot both pass a test like <c>Stock &gt;= 3</c>: this is how a counter is changed safely. The audit fields of the change are written, and what the caller may not see (the deleted, the other tenants') is not matched.
    /// </summary>
    Task<long> UpdateWhereAsync(Expression<Func<TEntity, bool>> filter, Func<UpdateDefinitionBuilder<TEntity>, UpdateDefinition<TEntity>> update, CancellationToken cancellationToken = default);
}
