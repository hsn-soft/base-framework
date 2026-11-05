using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Domain.Repositories;

/// <summary>
/// Runs several repository calls as one transaction: either everything the work saved is committed together, or none of it when it throws. The code that needs it (a domain service that sells a product and links the roles to the sale) depends on this and on
/// the repositories, never on a context of the persistence.
/// </summary>
/// <remarks>The work may run again from its start when the persistence retries after a failure that may pass, so it must read what it needs inside and must not use entities it got before it began.</remarks>
public interface IUnitOfWork
{
    /// <summary>Runs <paramref name="work"/> in a transaction and returns what it returns.</summary>
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> work, CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/>, which returns nothing, in a transaction.</summary>
    Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);
}
