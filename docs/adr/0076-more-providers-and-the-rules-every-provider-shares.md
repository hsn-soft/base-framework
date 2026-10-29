# ADR-0076: More providers (NLog, Azure, AWS, MySQL), and the rules every logging provider shares

- **Status:** Accepted
- **Date:** 2026-10-27 (follows [ADR-0003](0003-abstractions-separate-from-providers.md); changes where the rules of the Serilog provider live)

## Context

The framework offered one logging provider (Serilog), one blob store (MinIO) and two relational databases (PostgreSQL, SQL Server). A product that logs with NLog, keeps files in Azure Blob Storage or on Amazon S3, or runs on MySQL could not build on it, although the abstractions
(`ILogger`, `IBlobStore`, `AddBaseEfCore`) were made for it. The rules that make a log a log of the framework (its type, its `Facility`, what is noise, which place it goes to, how an object is masked) were written inside the Serilog provider, so a second provider would have copied them.

## Decision

- **`HsnSoft.Base.Logging` holds the rules every provider shares**, as pure functions: `LogShape` (the `LogType` of a category, the `Facility` of an event name, the noise fields of a log type, the names of the fields), `LogRouting` (which place a log goes to, from its level, category and type) and `SensitiveDataTree` (an
  object as a tree with the secrets masked; a provider turns the tree into its own values). The Serilog provider was changed to use them (its 136 tests are unchanged and pass), so the two providers cannot drift apart.
- **`HsnSoft.Base.Logging.NLog`** is the NLog provider: `AddBaseNLog(configuration, name)` reads the same settings as `AddBaseSerilog` (`Logging:LogLevel`, `PersistentLogger`) and writes the same console line, the same file line and the same fields (`LogType`, `MessageType`, `Facility`, a bounded number of fields, objects as one masked JSON field,
  text cut at `MaxPersistentFieldLength`), so a service moves between the two by changing one call. Graylog is written by a target of its own (GELF over HTTP, in a background queue that sends again what failed), which is why `SinkRefreshIntervalMinutes` does not apply. The service has its own NLog factory:
  no global `LogManager` configuration and no `nlog.config`. The message is rendered from the template with the secrets masked, as NLog would otherwise write an `{@Object}` into the message whole.
- **`HsnSoft.Base.BlobStoring.Azure`** (`AddAzureBlobStoring`, section `AzureBlob`: a connection string for Azurite or development, or the address of the account reached with the identity of the host) and **`HsnSoft.Base.BlobStoring.Aws`** (`AddAwsBlobStoring`, section `AwsS3`: a region, and the credentials from the
  host's role; a `ServiceUrl` reaches any store with the S3 API) implement `IBlobStore` beside MinIO. A container is made when the first blob is saved; a missing blob reads as `null`. The S3 bucket is looked for with a listing, not with a call that needs more rights.
- **`HsnSoft.Base.EntityFrameworkCore.MySql`** (`AddBaseMySql`, `UseMySql` for `AddBaseEfCore`, name `mysql`) on Oracle's `MySql.EntityFrameworkCore`: Pomelo has no release for EF Core 10 yet. The repository tests that matter (the atomic `UpdateWhere`, the inbox taken by one of twenty deliveries at once) run against a real MySQL.
- **One contract for every blob store:** the integration tests of MinIO, Azure (Azurite) and the S3 client (against MinIO) derive from one `BlobStoreContractTests`, so a store that passes is a store that behaves as the others.
- **A system is composed from the packages it wants, and that is checked:** the project graph of two different systems (the one on Azure, NLog, Kafka, MongoDB and PostgreSQL without tenants; the one on Amazon S3, Serilog, RabbitMQ and MySQL with tenants and without localization) was restored and the packages of every choice that was not
  made (Serilog, MinIO, Npgsql, Swashbuckle, ...) are not in it; and a host with NLog, Azure and MySQL runs against the real services (`AlternativeStackTests`).

## Consequences

- A product picks its logging library, its blob store and its database per service, and the other choices cost it nothing.
- Anything a provider must do the same way is in `Logging`, and a new provider is the conversion of those rules into its own library, not a copy of the Serilog one.
- NLog's Graylog target is ours (about a hundred lines); if Graylog needs more than GELF over HTTP (UDP, TCP), it is a new target.
- SQLite is not offered yet (it is the next provider of the same shape); neither is an OpenAPI package beside Swashbuckle (a host that uses `Microsoft.AspNetCore.OpenApi` registers it itself).

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0015](0015-logging-architecture.md), [ADR-0017](0017-two-places-two-levels.md), [ADR-0021](0021-sensitive-data-by-attributes.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md), [`docs/Logging.md`](../Logging.md).
