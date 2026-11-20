using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Concurrency.EventBus;

/// <summary>
/// The handling half of the hand-off: a message that carries a place (<see cref="ConcurrencyHandoffHeaders"/>) keeps it alive while its handler works (the lease is renewed three times in each lease time, so a job that takes longer than the lease does not lose its place) and **gives it back when the handling ends, whatever way
/// it ends** (handled, rejected, failed on every attempt): the place is the job's, and a job that ended is over. The lease is the safety when the pod dies: the place is free when it ends. A handling cut short by a shutdown gives nothing back (the message comes back, the lease runs out by itself).
/// A message with no place is not touched, and a place that has ended already (and may have been given to another) is never freed: the holder says whose it is.
/// </summary>
public sealed partial class ConcurrencyHandoffObserver(IConcurrencyLimiter limiter, ILogger<ConcurrencyHandoffObserver> logger, TimeProvider? time = null) : IEventHandlingObserver
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public ValueTask<IAsyncDisposable?> StartedAsync(ParentMessageEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!TryRead(envelope, out string name, out string holder, out TimeSpan leaseTime))
        {
            return ValueTask.FromResult<IAsyncDisposable?>(null);
        }

        return ValueTask.FromResult<IAsyncDisposable?>(new Heartbeat(limiter.Attach(name, holder), leaseTime, _time, logger));
    }

    public async ValueTask EndedAsync(ParentMessageEnvelope envelope, EventHandlingOutcome outcome, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!TryRead(envelope, out string name, out string holder, out _))
        {
            return;
        }

        await limiter.Attach(name, holder).DisposeAsync().ConfigureAwait(false);
        LogGivenBack(logger, name, outcome);
    }

    private static bool TryRead(ParentMessageEnvelope envelope, out string name, out string holder, out TimeSpan leaseTime)
    {
        name = envelope.GetHeader(ConcurrencyHandoffHeaders.Name) ?? string.Empty;
        holder = envelope.GetHeader(ConcurrencyHandoffHeaders.Holder) ?? string.Empty;
        leaseTime = long.TryParse(envelope.GetHeader(ConcurrencyHandoffHeaders.LeaseSeconds), NumberStyles.None, CultureInfo.InvariantCulture, out long seconds) && seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero;
        return name.Length > 0 && holder.Length > 0 && leaseTime > TimeSpan.Zero;
    }

    [LoggerMessage(EventId = 2100, EventName = "CONCURRENCY_HANDOFF_GIVEN_BACK", Level = LogLevel.Debug, Message = "The place of {Name} was given back: the handling ended {Outcome}")]
    private static partial void LogGivenBack(ILogger logger, string name, EventHandlingOutcome outcome);

    [LoggerMessage(EventId = 2101, EventName = "CONCURRENCY_HANDOFF_LOST", Level = LogLevel.Warning, Message = "The place of {Name} was not renewed: its lease had ended and the place may be another's (the lease is shorter than the work: make it longer)")]
    private static partial void LogLost(ILogger logger, string name);

    [LoggerMessage(EventId = 2102, EventName = "CONCURRENCY_HANDOFF_RENEW_ERROR", Level = LogLevel.Warning, Message = "The place of {Name} could not be renewed this time: it is tried again")]
    private static partial void LogRenewError(ILogger logger, Exception exception, string name);

    /// <summary>Renews the lease three times in each lease time until it is disposed (the handling ended); a lease that has ended is told once and not renewed again.</summary>
    private sealed class Heartbeat : IAsyncDisposable
    {
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;

        public Heartbeat(IConcurrencyLease lease, TimeSpan leaseTime, TimeProvider time, ILogger logger)
        {
            TimeSpan period = leaseTime / 3 < TimeSpan.FromMilliseconds(10) ? TimeSpan.FromMilliseconds(10) : leaseTime / 3;
            _loop = Task.Run(() => RunAsync(lease, leaseTime, period, time, logger, _stop.Token));
        }

        private static async Task RunAsync(IConcurrencyLease lease, TimeSpan leaseTime, TimeSpan period, TimeProvider time, ILogger logger, CancellationToken stop)
        {
            using PeriodicTimer timer = new(period, time);
            try
            {
                while (await timer.WaitForNextTickAsync(stop).ConfigureAwait(false))
                {
                    try
                    {
                        if (!await lease.RenewAsync(leaseTime, stop).ConfigureAwait(false))
                        {
                            LogLost(logger, lease.Name);
                            return;
                        }
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        LogRenewError(logger, exception, lease.Name);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // the handling ended
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync().ConfigureAwait(false);
            await _loop.ConfigureAwait(false);
            _stop.Dispose();
        }
    }
}
