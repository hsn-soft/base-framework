# ADR-0077: Only the Abc sample stays

- **Status:** Accepted
- **Note:** [ADR-0078](0078-the-edx-sample-a-file-intake-system.md) adds the Edx sample, the first of the "other samples" this ADR leaves room for.
- **Date:** 2026-10-27 (supersedes [ADR-0061](0061-the-xyz-sample-beside-abc.md); changes the sample folders of [ADR-0006](0006-repository-and-solution-layout.md))

## Context

The repository had four sample systems: Abc (the one the framework is shaped against), Xyz (the same job with other choices, Kafka and one project per service), Def (a monolith web api) and Agt (a scheduler agent). Xyz, Def and Agt were written to show that the framework fits more than one
product, and they did, but each framework change had to be carried into all of them (the tenants, the audit fields, the packages) and each one that falls behind is a sample that teaches the wrong thing. The proof that more than one product fits is now stronger and cheaper somewhere else: the dependency graph of two
different systems is restored and checked ([ADR-0076](0076-more-providers-and-the-rules-every-provider-shares.md)), a host on other choices runs against the real services (`AlternativeStackTests`), and every package has its own unit and integration tests. Other samples are planned, written on purpose, not carried over.

## Decision

- **`samples/xyz`, `samples/def` and `samples/agent` are removed**, with their entries in `HsnSoft.Base.sln` (the solution folders `xyz`, `def` and `agent` too) and the places that named them.
- **Abc is the one sample.** What `samples/xyz/run-all.sh` and `smoke-test.sh` did for the work "after a change that touches the samples" (`CLAUDE.md`) is done for Abc by **`samples/abc/run-all.sh`** (builds and starts the hosts of the order flow) and **`samples/abc/smoke-test.sh`** (the three outcomes of an order through the commercial gateway, each checked).
- **What only the removed samples exercised is still covered:** Kafka by `KafkaTests` and the Kafka unit tests (the framework's), the rate limit and the gateway shell were the products' own code and go with them; an agent that takes few packages and a monolith are compositions that the graph check in ADR-0076 shows.

## Consequences

- The samples folder is Abc and its documents ([samples/README.md](../../samples/README.md), [samples/abc/REFERENCES.md](../../samples/abc/REFERENCES.md)); a framework change is carried into one sample.
- There is no sample of a service on Kafka any more: the Kafka consumer (`fetchCount`, the offsets, the dead letters) is demonstrated by its tests, not by a running system. A new sample that wants it is added on purpose.
- The older ADRs that describe the removed samples are kept as history; they say so in a note.

## Related

[ADR-0006](0006-repository-and-solution-layout.md), [ADR-0061](0061-the-xyz-sample-beside-abc.md), [ADR-0062](0062-a-kafka-consumer-for-every-service-and-event.md), [ADR-0076](0076-more-providers-and-the-rules-every-provider-shares.md).
