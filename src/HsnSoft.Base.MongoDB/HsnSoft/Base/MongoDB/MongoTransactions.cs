using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>The transaction a unit of work of this async flow has open, if any: the repositories read and write through its session, so everything they do inside <see cref="MongoUnitOfWork"/> is one transaction.</summary>
internal static class MongoTransactionScope
{
    private static readonly AsyncLocal<IClientSessionHandle?> s_current = new();

    public static IClientSessionHandle? Current => s_current.Value;

    public static IDisposable Begin(IClientSessionHandle session)
    {
        IClientSessionHandle? before = s_current.Value;
        s_current.Value = session;
        return new Restore(before);
    }

    private sealed class Restore(IClientSessionHandle? before) : IDisposable
    {
        public void Dispose() => s_current.Value = before;
    }
}

internal static class MongoTransactions
{
    /// <summary>
    /// Runs <paramref name="work"/> in a transaction of the client: all it does through the repositories commits together or is rolled back together. A transaction that is already open (a unit of work inside a unit of work) is joined, not nested. MongoDB has
    /// transactions only on a replica set or a sharded cluster: on a standalone server this is a <see cref="NotSupportedException"/> that says so, not a failure of a command.
    /// </summary>
    public static async Task<TResult> RunAsync<TResult>(IMongoClient client, Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(work);

        if (MongoTransactionScope.Current is not null)
        {
            return await work(cancellationToken).ConfigureAwait(false);
        }

        using IClientSessionHandle session = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        try
        {
            using (MongoTransactionScope.Begin(session))
            {
                // the driver may run the callback again when the server asks to (a transient error of the transaction): the work has to be one that can be run again
                return await session.WithTransactionAsync((_, token) => work(token), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (IsStandalone(exception))
        {
            throw Unsupported(exception);
        }
    }

    // what a standalone server (or a driver that knows it is talking to one) says: the command is refused (code 20) or the driver refuses to send it
    private static bool IsStandalone(Exception exception)
        => exception is MongoCommandException { Code: 20 }
           || exception.Message.Contains("replica set", StringComparison.OrdinalIgnoreCase)
           || exception.Message.Contains("do not support transactions", StringComparison.OrdinalIgnoreCase);

    private static NotSupportedException Unsupported(Exception? inner)
        => new("This MongoDB server is a standalone one: a transaction needs a replica set (or a sharded cluster). Use the methods without a transaction, or run the database as a replica set.", inner);
}

/// <summary>
/// <see cref="IUnitOfWork"/> for MongoDB: <see cref="ExecuteAsync{TResult}"/> runs the work in one transaction, and every repository call it makes (reads too, so the work sees its own writes) is part of it. It needs a replica set (or a sharded cluster): on a
/// standalone MongoDB it throws <see cref="NotSupportedException"/>. A service that runs on a standalone MongoDB does not register it and uses the methods of the repository that need no transaction.
/// </summary>
public sealed class MongoUnitOfWork(IMongoClient client) : IUnitOfWork
{
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken = default)
        => MongoTransactions.RunAsync(client, work, cancellationToken);

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

/// <summary>Registers the transaction of MongoDB.</summary>
public static class MongoUnitOfWorkServiceCollectionExtensions
{
    /// <summary>Registers <see cref="MongoUnitOfWork"/> as the <see cref="IUnitOfWork"/> of the service (a replica set or a sharded cluster only).</summary>
    public static IServiceCollection AddMongoUnitOfWork(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddScoped<IUnitOfWork, MongoUnitOfWork>();
    }
}
