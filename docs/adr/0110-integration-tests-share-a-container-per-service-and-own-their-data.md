# ADR-0110: The integration tests share a container per service and own their data; a test that breaks a container owns the container

- **Status:** Accepted
- **Date:** 2026-10-30 (refines [ADR-0010](0010-integration-tests-with-testcontainers.md))

## Context

Most integration test classes started a container for every test: a run of the 85 tests started 72 containers (a database takes five to ten seconds to be ready, Kafka about six, and each was removed again). Of the 436 seconds of the integration step on the CI, 93 were tests and the rest was containers, and the same runs on a busy laptop
were the ones in which a database did not answer in time. The minutes of the CI runners are limited.

## Decision

- **One container per service for the whole run**, through xUnit collection fixtures (`Fixtures.cs`: PostgreSQL, MySQL, SQL Server, MongoDB, Redis, MinIO, Azurite, Kafka; RabbitMQ keeps its class fixture). The classes of a collection run one after the other, as every class of the assembly does (`DisableTestParallelization`), so nothing runs
  against a container at the same time as another test.
- **A test owns what it changes**: it asks the fixture for a database of its own (`NewConnectionString`: a new database name in the connection string, which the context creates; `NewUrl` for MongoDB) or uses a bucket, an exchange, a topic or a service name that nobody else has (a new `Guid` in the name). The connection string is made **once per test** and kept, not in the lambda
  that configures a context (that runs for every scope and would reach a different database each time). The SQL fixtures log in as an account that may create databases (`root` for MySQL).
- **A test that changes the container itself** (stops it, pauses it, kills its process or its connections, cuts its network, restarts it, or checks that nothing is left behind after it stops) **does not use a fixture**: it starts a container of its own and removes it, so the other tests never see a container that is broken. The tests of that kind are
  few and are the ones that cost a container each.
- The diagnostics before a container is removed (`Support.DisposeAsync`) ignore what a healthy container logs as part of the tests: the FATAL line of PostgreSQL when EF Core probes a database before creating it.

## Consequences

- 13 containers instead of 72 and **a run of the integration tests of 133 seconds instead of 327–436** on the same machine, 85 of 85 passed; the CI minutes of that job fall in the same proportion.
- A test that leaves a lot of data in a shared container makes the next ones slower; a test that changes server-wide state (a setting, a user, a global key) belongs in the second kind.
- Redis: the test of a service that starts before Redis never touched the container and no longer waits for one.

## Related

[ADR-0010](0010-integration-tests-with-testcontainers.md), [ADR-0109](0109-the-ci-jobs-are-a-pipeline-built-once.md).
