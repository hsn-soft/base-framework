using System;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.RateLimiting;

/// <summary>
/// A counter of the requests of one caller in a window of time, for every instance of a service: <c>permits</c> requests of one <c>key</c> (an address, a user) under one <c>name</c> (what is limited: <c>sign-in</c>) in a window that starts with the first request of the key.
/// A request that is over the limit is still counted, so a caller that keeps knocking stays refused until its window ends. <c>HsnSoft.Base.RateLimiting.StackExchangeRedis</c> is the implementation for a cluster; <see cref="InMemoryRateLimiter"/> counts in one process.
/// </summary>
public interface IRateLimiter
{
    /// <summary>Counts one request of <paramref name="key"/> under <paramref name="name"/> and says whether it is within the limit.</summary>
    /// <param name="name">What is limited (<c>sign-in</c>); a name is a limit of its own.</param>
    /// <param name="key">Who is limited under the name: an address, a user, a client; each key has its own count.</param>
    /// <param name="permits">How many requests the key may make in a window (at least 1).</param>
    /// <param name="window">How long a window lasts (longer than zero).</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    ValueTask<RateLimitResult> TryAcquireAsync(string name, string key, int permits, TimeSpan window, CancellationToken cancellationToken = default);
}

/// <summary>The answer of a count: whether the request is within the limit, how many are left in the window and when the window ends.</summary>
/// <param name="Allowed">The request is within the limit.</param>
/// <param name="Remaining">How many more requests the key may make in this window (0 when it is over).</param>
/// <param name="RetryAfter">How long until the window ends and the count starts again.</param>
public readonly record struct RateLimitResult(bool Allowed, int Remaining, TimeSpan RetryAfter);
