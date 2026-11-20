using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.RateLimiting;

/// <summary>
/// <see cref="IRateLimiter"/> in the memory of one process: the limit holds for this process only, so with several pods it is the limit of each. For a development machine, for tests and for a service that runs as one instance; a cluster uses
/// <c>HsnSoft.Base.RateLimiting.StackExchangeRedis</c>. A window starts with the first request of a key and ends after its time; the windows that have ended are dropped as the limiter is used, so the memory follows the callers of the last window.
/// </summary>
public sealed class InMemoryRateLimiter(TimeProvider? time = null) : IRateLimiter
{
    // the ended windows are swept every this many requests
    private const int SweepEvery = 1024;

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Dictionary<string, Window> _windows = [with(StringComparer.Ordinal)];
    private readonly Lock _gate = new();
    private int _requests;

    public ValueTask<RateLimitResult> TryAcquireAsync(string name, string key, int permits, TimeSpan window, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(permits, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset now = _time.GetUtcNow();
        string id = name + "\n" + key;
        lock (_gate)
        {
            if (++_requests % SweepEvery == 0)
            {
                Sweep(now);
            }

            if (!_windows.TryGetValue(id, out Window? current) || current.Ends <= now)
            {
                current = new Window(now + window);
                _windows[id] = current;
            }

            current.Count++;
            bool allowed = current.Count <= permits;
            return ValueTask.FromResult(new RateLimitResult(allowed, Math.Max(0, permits - current.Count), current.Ends - now));
        }
    }

    private void Sweep(DateTimeOffset now)
    {
        foreach (string ended in new List<string>(_windows.Keys))
        {
            if (_windows[ended].Ends <= now)
            {
                _windows.Remove(ended);
            }
        }
    }

    private sealed class Window(DateTimeOffset ends)
    {
        public DateTimeOffset Ends { get; } = ends;

        public int Count { get; set; }
    }
}
