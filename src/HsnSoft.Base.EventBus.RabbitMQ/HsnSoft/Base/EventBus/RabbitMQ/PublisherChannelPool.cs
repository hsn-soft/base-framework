using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// The channels a publisher keeps open between messages. Opening a channel (with confirms) and closing it is two round trips to the broker and a channel made and destroyed for every message; a service that publishes a few hundred messages a second spent
/// more on that than on the messages. A message takes a channel from the pool for its publish (one publisher at a time on a channel) and gives it back when it was confirmed. A channel that is not open any more, or on which a publish failed (a timeout, a nack:
/// what state it is in is not known), is closed and not given back, and the pool keeps at most <c>size</c> idle channels (what is over is closed). A size of 0 is no pool: a channel for each message, as before.
/// </summary>
internal sealed class PublisherChannelPool(Func<CancellationToken, Task<IChannel>> create, int size) : IAsyncDisposable
{
    private readonly ConcurrentQueue<PooledChannel> _idle = new();
    private int _idleCount;
    private bool _disposed;

    /// <summary>How many channels wait in the pool.</summary>
    public int Idle => Volatile.Read(ref _idleCount);

    /// <summary>A channel for one publish: an idle one that is still open, or a new one.</summary>
    public async Task<PublisherChannelLease> RentAsync(CancellationToken cancellationToken)
    {
        while (_idle.TryDequeue(out PooledChannel? pooled))
        {
            Interlocked.Decrement(ref _idleCount);
            if (pooled.Channel.IsOpen)
            {
                return new PublisherChannelLease(this, pooled);
            }

            await CloseAsync(pooled).ConfigureAwait(false);
        }

        return new PublisherChannelLease(this, new PooledChannel(await create(cancellationToken).ConfigureAwait(false)));
    }

    internal async ValueTask ReleaseAsync(PooledChannel pooled, bool reusable)
    {
        if (reusable && size > 0 && !_disposed && pooled.Channel.IsOpen && Interlocked.Increment(ref _idleCount) <= size)
        {
            _idle.Enqueue(pooled);
            if (_disposed)
            {
                await DrainAsync().ConfigureAwait(false);   // disposed while it was being given back
            }

            return;
        }

        if (reusable && size > 0 && !_disposed && pooled.Channel.IsOpen)
        {
            Interlocked.Decrement(ref _idleCount);   // the pool was full
        }

        await CloseAsync(pooled).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await DrainAsync().ConfigureAwait(false);
    }

    private async ValueTask DrainAsync()
    {
        while (_idle.TryDequeue(out PooledChannel? pooled))
        {
            Interlocked.Decrement(ref _idleCount);
            await CloseAsync(pooled).ConfigureAwait(false);
        }
    }

    private static async ValueTask CloseAsync(PooledChannel pooled)
    {
        try
        {
            if (pooled.Channel.IsOpen)
            {
                await pooled.Channel.CloseAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is IOException or AlreadyClosedException or TimeoutException or OperationCanceledException)
        {
            // it is going away anyway
        }
        finally
        {
            pooled.Channel.Dispose();
        }
    }
}

/// <summary>A channel with what this publisher already declared on it (the exchanges and queues: declaring is a round trip, and once per channel is enough).</summary>
internal sealed class PooledChannel(IChannel channel)
{
    public IChannel Channel { get; } = channel;

    public HashSet<string> Declared { get; } = [];
}

/// <summary>A channel of the pool for one publish. <see cref="Succeed"/> says the publish was confirmed, so the channel may be used again; a lease that is disposed without it closes the channel.</summary>
internal sealed class PublisherChannelLease : IAsyncDisposable
{
    private readonly PublisherChannelPool _pool;
    private readonly PooledChannel _pooled;
    private bool _succeeded;

    internal PublisherChannelLease(PublisherChannelPool pool, PooledChannel pooled)
    {
        _pool = pool;
        _pooled = pooled;
    }

    public IChannel Channel => _pooled.Channel;

    /// <summary>The exchanges and queues already declared on this channel.</summary>
    public ISet<string> Declared => _pooled.Declared;

    public void Succeed() => _succeeded = true;

    public ValueTask DisposeAsync() => _pool.ReleaseAsync(_pooled, _succeeded);
}
