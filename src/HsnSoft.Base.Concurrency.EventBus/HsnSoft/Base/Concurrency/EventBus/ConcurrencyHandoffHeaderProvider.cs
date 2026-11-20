using System.Collections.Generic;
using System.Globalization;
using HsnSoft.Base.EventBus;

namespace HsnSoft.Base.Concurrency.EventBus;

/// <summary>
/// The publishing half of the hand-off: when an endpoint with <c>[ConcurrencyLimit(HandOff = true)]</c> publishes an event, the **first** event of its flow carries the place it took (<see cref="ConcurrencyHandoffHeaders"/>), and from then the endpoint does not give the place back.
/// The headers are local to that message (<c>Local-</c>): the events its handler publishes do not carry the place on.
/// </summary>
public sealed class ConcurrencyHandoffHeaderProvider : IEventHeaderProvider
{
    public string? GetCorrelationId() => null;

    public void AddHeaders(IDictionary<string, string> headers)
    {
        System.ArgumentNullException.ThrowIfNull(headers);

        if (ConcurrencyHandoffContext.TryTake() is not { } handoff)
        {
            return;
        }

        headers[ConcurrencyHandoffHeaders.Name] = handoff.Name;
        headers[ConcurrencyHandoffHeaders.Holder] = handoff.Holder;
        headers[ConcurrencyHandoffHeaders.LeaseSeconds] = ((long)System.Math.Ceiling(handoff.LeaseTime.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
    }
}
