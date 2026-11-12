using System;
using System.Threading;

namespace HsnSoft.Base.Concurrency;

/// <summary>
/// The names of the headers that carry a place from the endpoint that took it to the handler that gives it back. They start with <c>Local-</c>: a header of that kind belongs to one message and is not passed on to the messages it causes (<c>ParentMessageEnvelope.LocalHeaderPrefix</c> of the event bus),
/// so the handler of the first event does not hand the same place to the events it publishes.
/// </summary>
public static class ConcurrencyHandoffHeaders
{
    /// <summary>What is limited (the name of the place).</summary>
    public const string Name = "Local-ConcurrencyName";

    /// <summary>Which place among the holders of the name (<see cref="IConcurrencyLease.Holder"/>).</summary>
    public const string Holder = "Local-ConcurrencyHolder";

    /// <summary>How long the lease is, in seconds: the handler renews it by this much while it works.</summary>
    public const string LeaseSeconds = "Local-ConcurrencyLeaseSeconds";
}

/// <summary>What an endpoint hands over with the event it publishes: the place it took and how long its lease is.</summary>
public sealed record ConcurrencyHandoff(string Name, string Holder, TimeSpan LeaseTime);

/// <summary>
/// The place an endpoint (<c>[ConcurrencyLimit(HandOff = true)]</c>) holds for the event it is going to publish. While the endpoint runs, the **first** event published in its flow takes the place (<see cref="ConcurrencyHandoffContext.TryTake"/>): from then the endpoint does not give it back,
/// the handler of that event does. When no event takes it (the action published nothing, or failed before it did) the endpoint gives the place back itself.
/// </summary>
public sealed class ConcurrencyHandoffSlot : IDisposable
{
    private readonly ConcurrencyHandoff _handoff;
    private readonly ConcurrencyHandoffSlot? _previous;
    private int _taken;

    internal ConcurrencyHandoffSlot(ConcurrencyHandoff handoff, ConcurrencyHandoffSlot? previous)
    {
        _handoff = handoff;
        _previous = previous;
    }

    /// <summary>Whether an event took the place (it is then the event's to give back).</summary>
    public bool WasTaken => Volatile.Read(ref _taken) == 1;

    internal ConcurrencyHandoff? TryTake() => Interlocked.CompareExchange(ref _taken, 1, 0) == 0 ? _handoff : null;

    public void Dispose() => ConcurrencyHandoffContext.Close(_previous);
}

/// <summary>The slot of the endpoint that is running (it follows the async calls of the request), for the publisher of an event to take the place from.</summary>
public static class ConcurrencyHandoffContext
{
    private static readonly AsyncLocal<ConcurrencyHandoffSlot?> s_current = new();

    /// <summary>Opens the slot for the place <paramref name="lease"/> until the returned slot is disposed.</summary>
    public static ConcurrencyHandoffSlot Open(IConcurrencyLease lease, TimeSpan leaseTime)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseTime, TimeSpan.Zero);

        ConcurrencyHandoffSlot slot = new(new ConcurrencyHandoff(lease.Name, lease.Holder, leaseTime), s_current.Value);
        s_current.Value = slot;
        return slot;
    }

    /// <summary>The place of the endpoint that is running, taken by the caller (the first one that asks gets it; the next ones get <see langword="null"/>), or <see langword="null"/> when no endpoint hands a place over.</summary>
    public static ConcurrencyHandoff? TryTake() => s_current.Value?.TryTake();

    internal static void Close(ConcurrencyHandoffSlot? previous) => s_current.Value = previous;
}
