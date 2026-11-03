---
paths:
  - "src/**"
  - "test/**"
  - "HsnSoft.Base.sln"
  - "common*.props"
---

# Framework architecture

- Keep the framework product-neutral. Put reusable infrastructure in a one-capability package; put product-specific shared behavior in that product's shared project and business rules in the service.
- Keep contracts separate from providers. A provider depends on its abstraction, never the reverse. Optional capabilities stay opt-in and must not pull unrelated dependencies into consumers.
- Keep capabilities and providers independently consumable. `EventBus` is the shared contract; `EventBus.Kafka` and `EventBus.RabbitMQ` are separate provider packages. Never put both brokers in one package; apply the same rule to database, logging and blob-storage providers.
- Consumers can compose the providers they need (for example Kafka + MongoDB + Redis + MinIO, or RabbitMQ + PostgreSQL) by referencing those individual packages. Do not make a package pull a technology it does not use or add a dependency just to make one consumer convenient.
- Keep the package graph acyclic. Every framework project must reach `HsnSoft.Base.Core`, but must not repeat a framework dependency already brought by a referenced sibling. A package must reference dependencies exposed by its public API.
- Put code in the framework only when it is product-independent. Use `docs/Overview.md` as the package/dependency map; do not create a parallel catalog.
- Keep domain/application abstractions independent of persistence providers. Repository contracts must not expose provider contexts, `IQueryable`, or eager-loading details; follow the existing store-specific repository and projection patterns.
- Database provider configuration belongs to each context's properties class; do not revive the superseded `Database:Provider` setting.
- To add a package or a provider, use the `add-package` or `add-provider` skill.
- Order `.csproj` references in separate alphabetized groups: external NuGet/FrameworkReference, framework (`HsnSoft.Base.*`), product shared (`<Product>.Shared.*`), then own projects. Use the repository fixer/checks; do not reorder by preference.
- Samples are consumers, never a reason to combine providers or add framework dependencies; their rules are in `repository-layout.md` and `samples/CLAUDE.md`.

Relevant decisions: ADR-0002–0006, ADR-0042–0046, ADR-0050, ADR-0070–0075. ADR-0050 supersedes the provider-selection setting from ADR-0047. See also `CONTRIBUTING.md` (Adding a package) and `docs/Overview.md`.
