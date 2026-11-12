# ADR-0159: A place of a concurrency limit is handed over with an event, so a job answers at once and is over when its handler is

- **Status:** Accepted (builds on [ADR-0143](0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md))
- **Date:** 2026-11-12

## Context

A job of the scheduler must not overlap itself in any pod ([ADR-0143](0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md): `[ConcurrencyLimit]` holds a place until the answer is written). A job that takes a quarter of an hour cannot be an HTTP request the scheduler waits for. The wanted shape: **the endpoint takes the place and answers at once, and publishes an event; the handler of the event
does the work, and the place is free again when the work ends.** The previous framework did this with two attributes of its shared library (`EndpointConcurrencyLimit` counted while the request ran, `EventConcurrencyLimit` counted and left the count to the consumer to take down, with a time to live as the safety). Two things were wrong with it: a counter has no owner (a consumer that is late takes down the count of another run,
and the count drifts), and nothing kept the count alive for work longer than its time to live.

## Decision

- **The place has an owner and travels with the event.** `IConcurrencyLease.Holder` is the identity of a place among the holders of a name, and `IConcurrencyLimiter.Attach(name, holder)` makes the lease of a place another process took: the process that gets the holder can **renew** it and **give it back**, and a place that has ended is not renewed and giving it back does nothing — so a holder that arrives late
  (a message delivered again) never frees the place of another run. Nothing is asked of the store to attach.
- **`[ConcurrencyLimit(name, max, HandOff = true)]`** (and `RequireConcurrencyLimit(..., handOff: true)`): the place is **not** given back when the answer is written. The first event published in the flow of the endpoint takes it (`ConcurrencyHandoffContext`: an `AsyncLocal` slot the endpoint opens); when no event takes it, or the endpoint fails, the endpoint gives the place back itself
  (a place is never held for a handler that never comes: the broker may have refused the event). The lease of the attribute is the longest the **handler** is expected to take.
- **`HsnSoft.Base.Concurrency.EventBus`** (a package of its own: it joins the two capabilities, and a service that needs only one does not pull the other) carries the place in **three headers** on that one message (`Local-ConcurrencyName`, `Local-ConcurrencyHolder`, `Local-ConcurrencyLeaseSeconds`, by an `IEventHeaderProvider`) and, at the other end, an
  **`IEventHandlingObserver`** keeps it alive while the handler works (the lease is renewed three times in each lease time) and **gives it back when the handling ends, handled, rejected or failed on every attempt**: the place is the job's, and a job that ended is over, so a failure does not hold the place for the rest of its lease. The lease remains the safety for a pod that dies. A handling cut short by a shutdown
  gives nothing back: its message comes back, and the lease runs out by itself if it does not. `services.AddConcurrencyHandoff()` registers both halves; the publisher and the handler may be one service, as a job is.
- **The event bus learns two small things**, product-neutral: `IEventHandlingObserver` (begin and end of the **whole handling of one message**, with its outcome; a delivery that is left alone by the inbox is not observed; an observer that fails never fails the handling), and the header prefix **`Local-`** (`ParentMessageEnvelope.LocalHeaderPrefix`): a header of that kind belongs to one message and is not passed on
  to the messages its handler publishes (otherwise every event the job causes would carry the place on).

## Consequences

- A job is: an endpoint with `HandOff = true` that publishes one event and answers at once, and a handler that does the work. The scheduler's next call while the work runs is answered 429, as before, and the place is free the moment the handler ends.
- A message delivered again (a requeue) carries a holder that owns nothing by then: it renews nothing and frees nothing.
- The handler of a job runs as long as the broker lets a delivery stay unacknowledged (RabbitMQ: `consumer_timeout`, 30 minutes by default): a job longer than that is delivered again.
- It is a one-to-one hand-over: only the first event of the endpoint carries the place; a job that must publish several events publishes the one that does the work first.

## Related

[ADR-0143](0143-a-limit-on-how-many-run-at-once-holds-across-every-pod.md); [Concurrency](../Concurrency.md); ADR-0003, ADR-0133.
