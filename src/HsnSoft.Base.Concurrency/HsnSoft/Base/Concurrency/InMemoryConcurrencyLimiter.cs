using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.Concurrency;

/// <summary>
/// <see cref="IConcurrencyLimiter"/> in the memory of one process: the limit holds for this process only, so with several pods it is the limit of each. For a development machine, for tests and for a service that runs as one instance;
/// a cluster uses <c>HsnSoft.Base.Concurrency.StackExchangeRedis</c>.
/// </summary>
public sealed class InMemoryConcurrencyLimiter(TimeProvider? time = null) : IConcurrencyLimiter
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly Dictionary<string, Dictionary<Guid, DateTimeOffset>> _holders = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public ValueTask<IConcurrencyLease?> TryAcquireAsync(string name, int maxConcurrent, TimeSpan leaseTime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrent, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseTime, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset now = _time.GetUtcNow();
        lock (_gate)
        {
            if (!_holders.TryGetValue(name, out Dictionary<Guid, DateTimeOffset>? holders))
            {
                holders = [];
                _holders[name] = holders;
            }

            foreach (Guid expired in holders.Where(holder => holder.Value <= now).Select(holder => holder.Key).ToList())
            {
                holders.Remove(expired);
            }

            if (holders.Count >= maxConcurrent)
            {
                return ValueTask.FromResult<IConcurrencyLease?>(null);
            }

            Guid id = Guid.NewGuid();
            holders[id] = now + leaseTime;
            return ValueTask.FromResult<IConcurrencyLease?>(new Lease(this, name, id));
        }
    }

    private bool Renew(string name, Guid id, TimeSpan leaseTime)
    {
        DateTimeOffset now = _time.GetUtcNow();
        lock (_gate)
        {
            if (_holders.TryGetValue(name, out Dictionary<Guid, DateTimeOffset>? holders) && holders.TryGetValue(id, out DateTimeOffset until) && until > now)
            {
                holders[id] = now + leaseTime;
                return true;
            }

            return false;
        }
    }

    private void Release(string name, Guid id)
    {
        lock (_gate)
        {
            if (_holders.TryGetValue(name, out Dictionary<Guid, DateTimeOffset>? holders))
            {
                holders.Remove(id);
            }
        }
    }

    private sealed class Lease(InMemoryConcurrencyLimiter owner, string name, Guid id) : IConcurrencyLease
    {
        public string Name => name;

        public ValueTask<bool> RenewAsync(TimeSpan leaseTime, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseTime, TimeSpan.Zero);

            return ValueTask.FromResult(owner.Renew(name, id, leaseTime));
        }

        public ValueTask DisposeAsync()
        {
            owner.Release(name, id);
            return ValueTask.CompletedTask;
        }
    }
}
