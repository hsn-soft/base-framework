# ADR-0168: The name of a connection string has no dash, because it is also the name of an environment variable

- **Status:** Accepted (builds on [ADR-0050](0050-a-context-has-a-properties-class-for-its-database.md))
- **Date:** 2026-11-28

## Context

A connection string is read as `ConnectionStrings:<Name>`, and in a container or a shell it is the environment variable `ConnectionStrings__<Name>`. The names of the services were `Mongo-ContentService` and `EfCore-IdentityService`. A dash is not allowed in the name of a variable of a shell: `export "ConnectionStrings__Mongo-ContentService=..."` fails with "not a valid identifier", so a script that loads an `.env` with `export` (the detector, the test scripts, `source .env`) stopped on the first such line, while .NET itself read the name without a problem. A developer who put the override of a connection into their `.env`, as the guides say, could not run the checks.

## Decision

- **The name of a connection string is letters and digits only**: `<Provider><Service>`, `MongoContentService`, `EfCoreIdentityService`, `MongoVideoGeneratorService`. It is the value of the `ConnectionStringName` constant of the properties class of a context (ADR-0050), of `SeedStartupConnections`, and the key of `ConnectionStrings` in every settings file, `.env`, `settings.env` and Kubernetes secret.
- Every repository of the system (the services, the gateways, the auth server, the scheduler, the GitOps repository and the samples of this one) uses the new names; the tests, the documents and the examples of this repository were changed with it. Ownership by `SeedStartupConnections` is compared without regard to case, as before.
- The framework does not check the name (a name that has a dash still works for .NET); the rule is kept by the documents and by review.

## Consequences

A running environment that has the old names needs its secrets and its files changed **before** the images that read the new names: a pod with the new image and the old secret has no connection string and does not start. The old keys may stay until every pod runs the new image.

## Related

[ADR-0050](0050-a-context-has-a-properties-class-for-its-database.md), [docs/DatabaseStartup.md](../DatabaseStartup.md).
