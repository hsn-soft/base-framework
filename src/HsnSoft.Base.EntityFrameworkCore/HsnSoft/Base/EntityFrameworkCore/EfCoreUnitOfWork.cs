using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>The transaction of a context: <see cref="IUnitOfWork"/> over <see cref="DbContextExtensions.ExecuteInTransactionAsync{TResult}"/>. What the context tracked before the work is forgotten when it begins (it may run again after a retry).</summary>
public sealed class EfCoreUnitOfWork<TContext>(TContext db) : IUnitOfWork
    where TContext : DbContext
{
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        return db.ExecuteInTransactionAsync(token =>
        {
            db.ChangeTracker.Clear();
            return work(token);
        }, cancellationToken);
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(work);

        return ExecuteAsync<bool>(async token =>
        {
            await work(token).ConfigureAwait(false);
            return true;
        }, cancellationToken);
    }
}

/// <summary>Registers the transaction of a context.</summary>
public static class EfCoreUnitOfWorkServiceCollectionExtensions
{
    /// <summary>Registers <see cref="EfCoreUnitOfWork{TContext}"/> as the <see cref="IUnitOfWork"/> of the service.</summary>
    public static IServiceCollection AddEfCoreUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IUnitOfWork, EfCoreUnitOfWork<TContext>>();
    }
}
