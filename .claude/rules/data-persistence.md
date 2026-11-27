---
paths:
  - "src/HsnSoft.Base.Data/**"
  - "src/HsnSoft.Base.Ddd.*/**"
  - "src/HsnSoft.Base.EntityFrameworkCore*/**"
  - "src/HsnSoft.Base.MongoDB/**"
  - "src/HsnSoft.Base.EventBus.EntityFrameworkCore/**"
  - "src/HsnSoft.Base.EventBus.MongoDB/**"
  - "src/HsnSoft.Base.MultiTenancy.EntityFrameworkCore/**"
  - "src/HsnSoft.Base.MultiTenancy.MongoDB/**"
  - "docs/DatabaseStartup.md"
  - "docs/EntityConcepts.md"
---

# Data and persistence

- Keep store-neutral data filters and domain contracts separate from persistence providers. A service opts into a provider; a provider must not force unrelated stores or capabilities on consumers.
- Keep repository contracts free of `IQueryable`, database contexts and provider-specific eager-loading APIs. Use the existing relational/document repository layers and DTO projection patterns.
- Use the context's properties class for its database configuration; the old `Database:Provider` selector is superseded. Shared provider versions belong in `Directory.Packages.props`.
- Apply audit, soft-delete, and tenant concepts through the established entity concepts and persistence interceptors/filters. Enforce tenant-or-scope isolation, never both.
- Only the host that owns a database prepares it. Preserve the per-database startup lock and ordered startup steps. Use `AddMigrationStartupStep` for evolving schemas rather than adding migrations to the schema-creation step.
- Register seeders as ordered, idempotent database startup steps after schema setup. Preserve the lock and the `Seeding:Disabled` opt-out.
- Store an enum of a relational model by its number: no `HasConversion<string>()` or converter to text, no max length for it (ADR-0148). A document store keeps what its serializer says.
- A normalized field (`StringHelper.Normalize`) is never empty (entity guard, required column, check `<> ''`), is unique in the set the service identifies the row by, and every create/update that can break a set asks the repository first and throws the business duplicate exception; queries match an `Id` or a normalized field, and a repository method that takes a text takes a normalized one (`normalizedEmail`) (ADR-0150).
- Keep database command logging opt-in and infrastructure waits/retries bounded and safe to retry. Do not log sensitive data.
- Integration tests use real databases, own their data, and follow the shared Testcontainers lifecycle. Read [DatabaseStartup.md](../../docs/DatabaseStartup.md) and [EntityConcepts.md](../../docs/EntityConcepts.md) for implementation detail.

- **A repository write says what it did and fails with `RepositoryException`** ([ADR-0162](../../docs/adr/0162-a-repository-write-says-what-it-did-and-fails-with-one-exception-that-names-the-records.md)): `AddAsync` gives the same instance, `UpdateAsync` a `WriteOutcome`, `UpdateManyAsync` a `WriteResult`; a missing record is 404, a stale one and a duplicate of a unique key or index 409 (`DuplicateKey`, the name of the index in `Constraint`, never the value), each naming every record that failed (`Ids`); an empty `Guid` key is given by the repository. A write of many fails as a whole when one record fails.
- **MongoDB transactions are `MongoUnitOfWork`** and the `...InTransactionAsync` methods of `IMongoRepository`, for a replica set only ([ADR-0163](../../docs/adr/0163-mongodb-transactions-are-a-unit-of-work-and-a-write-of-many-without-one-is-all-or-none-as-far-as-a-document-store-can.md)): the plain many-methods need no transaction and do what a document store can; a test of a transaction runs on `MongoReplicaSetFixture`.
- **`tlsCAFile` of a MongoDB connection string is applied by `MongoOptions.Apply`** because the .NET driver ignores it ([ADR-0167](../../docs/adr/0167-the-tlscafile-of-a-mongodb-connection-string-is-applied-by-the-framework-because-the-net-driver-ignores-it.md)): the server certificate must chain to the CA of the file and match the host; `tlsInsecure` is the only way to turn the check off; a test of it uses a real MongoDB with TLS (`MongoTlsFixture`).

- **The name of a connection string is letters and digits** (`MongoContentService`, `EfCoreIdentityService`), never with a dash: it is also an environment variable (ADR-0168).

Relevant decisions: ADR-0004, ADR-0037, ADR-0042, ADR-0045–0048, ADR-0050, ADR-0067, ADR-0073–0075, ADR-0102, ADR-0105, ADR-0110, ADR-0118–0119, ADR-0124–0125, ADR-0128–0131, ADR-0136, ADR-0148, ADR-0150, ADR-0162, ADR-0163, ADR-0167, ADR-0168.
