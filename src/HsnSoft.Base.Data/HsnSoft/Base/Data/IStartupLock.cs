using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Data;

/// <summary>
/// A lock on the server of a database that one process holds at a time: the steps that prepare the database at startup (the schema, the seed data) run one after the other under it, whichever pods or hosts start together.
/// A database offers its own (<c>sp_getapplock</c> of SQL Server, an advisory lock of PostgreSQL, <c>GET_LOCK</c> of MySQL, a lease document of MongoDB), so no other service is needed; a lock is released when its
/// holder disposes it, and when the holder dies (the connection closes, or the lease ends).
/// </summary>
public interface IStartupLock
{
    /// <summary>Waits for the lock called <paramref name="name"/> and holds it until the result is disposed; throws <see cref="TimeoutException"/> when it is not free within <paramref name="timeout"/>.</summary>
    Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken);
}
