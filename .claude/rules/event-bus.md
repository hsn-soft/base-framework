---
paths:
  - "src/HsnSoft.Base.EventBus*/**"
  - "src/HsnSoft.Base.MultiTenancy.EventBus/**"
  - "docs/EventBus.md"
---

# Event bus and broker providers

- Keep the event-bus contract independent of brokers; RabbitMQ and Kafka are providers. Preserve the production wire format and topology. Publish events and send commands according to the existing envelope semantics.
- Keep caller identity and correlation/chain context in the message envelope and preserve existing naming and retry behavior. Tenant identity belongs in the established message headers, not the envelope; scope keys travel in the event body, not tenant headers.
- Preserve broker-specific delivery guarantees: RabbitMQ confirms publishes and handles unacknowledged/retried messages as documented; Kafka topic, consumer-group, backoff, timeout and parallel-fetch rules are deliberate.
- Do not create a connection, channel, or client per message or health probe. Reuse the managed clients and bounded waits; keep broker callbacks from blocking the thread pool.
- Keep the inbox opt-in and provider-specific. Preserve duplicate handling and cleanup semantics; integration tests must exercise the real broker and own their data/container lifecycle.
- Tenant-aware handlers must execute in the message tenant context and propagate the established tenant headers.
- Read [EventBus.md](../../docs/EventBus.md) and the cited ADRs before changing wire behavior, retry/ack behavior, topology, or consumer lifecycle.

Relevant decisions: ADR-0023, ADR-0057, ADR-0062–0067, ADR-0098–0104, ADR-0108, ADR-0110, ADR-0119, ADR-0127, ADR-0128.
