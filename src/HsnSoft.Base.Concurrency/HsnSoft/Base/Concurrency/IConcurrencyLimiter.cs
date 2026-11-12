using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Concurrency;

/// <summary>
/// A named semaphore for every instance of a service: at most <c>maxConcurrent</c> holders of a name at the same time, in all the pods together. A holder has a <b>lease</b> that ends by itself after its time, so a pod that dies while it holds one
/// does not hold it for ever. <c>HsnSoft.Base.Concurrency.StackExchangeRedis</c> is the implementation for a cluster; <see cref="InMemoryConcurrencyLimiter"/> counts in one process.
/// </summary>
public interface IConcurrencyLimiter
{
    /// <summary>
    /// A place among <paramref name="maxConcurrent"/>, or <see langword="null"/> when they are all taken (the caller says "too many", it does not wait). Dispose the lease when the work is done.
    /// </summary>
    /// <param name="name">What is limited (<c>send-invoice</c>); a name is a limit of its own.</param>
    /// <param name="maxConcurrent">How many may hold the name at once (at least 1).</param>
    /// <param name="leaseTime">How long a holder may hold it without renewing: the longest the work is expected to take.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    ValueTask<IConcurrencyLease?> TryAcquireAsync(string name, int maxConcurrent, TimeSpan leaseTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// The lease of a place that <b>another process took and handed over</b> (an endpoint takes a place, publishes an event and the handler of the event gives the place back: see <see cref="ConcurrencyHandoffHeaders"/>). It is made from the name and the <see cref="IConcurrencyLease.Holder"/>
    /// of the place; nothing is asked of the store here. A place that has ended is not renewed (<see langword="false"/>) and giving it back does nothing, so a holder that arrives late never frees a place that was given to another.
    /// </summary>
    /// <param name="name">What is limited.</param>
    /// <param name="holder">The identity of the place, as the lease that took it said (<see cref="IConcurrencyLease.Holder"/>).</param>
    IConcurrencyLease Attach(string name, string holder);
}

/// <summary>A place among the holders of a name. Disposing it gives the place back.</summary>
public interface IConcurrencyLease : IAsyncDisposable
{
    /// <summary>What is limited.</summary>
    string Name { get; }

    /// <summary>The identity of this place among the holders of the name: what travels with an event when the place is handed over (<see cref="IConcurrencyLimiter.Attach"/>).</summary>
    string Holder { get; }

    /// <summary>Makes the lease last <paramref name="leaseTime"/> more from now (work that takes longer than expected); <see langword="false"/> when the lease had ended already (the place was given to another).</summary>
    ValueTask<bool> RenewAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default);
}
