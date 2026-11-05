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

Relevant decisions: ADR-0004, ADR-0037, ADR-0042, ADR-0045–0048, ADR-0050, ADR-0067, ADR-0073–0075, ADR-0102, ADR-0105, ADR-0110, ADR-0118–0119, ADR-0124–0125, ADR-0128–0131, ADR-0136, ADR-0148, ADR-0150.
