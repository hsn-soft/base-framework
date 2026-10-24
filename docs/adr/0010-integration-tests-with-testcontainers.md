# ADR-0010: Integration tests start the real services with Testcontainers

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

The code that talks to RabbitMQ, Kafka, Redis, MongoDB, PostgreSQL, SQL Server and MinIO cannot be trusted from mocks: a topology declared with the wrong arguments, a serializer that differs on the
wire, a retry that never reconnects are only found against the real service. Mocks also made the 99 % coverage number meaningless for provider code.

## Decision

- **One project, `test/HsnSoft.Base.IntegrationTests`**, with a folder per area (`EventBus`, `Caching`, `Data`, `BlobStoring`). **Every provider has integration tests** against the real service, started in Docker by
  **Testcontainers**; the tests need a running Docker and nothing else installed.
- Containers are heavy, so test parallelization is **disabled** for this assembly (`DisableTestParallelization`): one test class after the other.
- A new provider's pull request includes its integration tests.
- **The samples are not run by CI or by the detector, only built.** Running them needs the whole local infrastructure and several processes. This has a cost we accepted knowingly: bugs that only appear when the
  hosts run together (a body read synchronously when a service is unreachable, a `[property: Required]` on a record) were found by running the samples against the local Graylog and by `smoke-test.sh`, not by the tests.
  Such a bug gets a unit or integration test as well once it is understood.

## Consequences

- The integration job needs Docker (the CI runner has it) and takes about a minute; locally the detector runs it unless `--skip-integration`.
- SQL Server needs about 2 GB of memory in Docker.
- The coverage of provider code comes from these tests (see [ADR-0011](0011-coverage-gate.md)).

## Related

[ADR-0009](0009-unit-tests-every-package.md), [ADR-0011](0011-coverage-gate.md), [ADR-0014](0014-detector-before-a-pull-request.md), `test/HsnSoft.Base.IntegrationTests`.
