---
paths:
  - "src/HsnSoft.Base.Ddd.*/**"
  - "src/HsnSoft.Base.Core/**"
  - "src/HsnSoft.Base.Auditing/**"
  - "docs/DddBuildingBlocks.md"
  - "docs/EntityConcepts.md"
---

# Domain and application building blocks

- Keep domain and application packages aligned with their layers. Domain entities and repository contracts belong in the domain layer; application DTOs and paging models belong in the application layer.
- Reuse the framework's entity and audit/soft-delete contracts. Keep audit values assigned through the established current-user/clock concepts and persistence interceptors rather than duplicating them in providers.
- Build DTO reads with the existing projection expressions; do not introduce a mapper or load full entity graphs when a projection suffices.
- Keep repository APIs store-neutral at the shared level, then use the existing relational/document-specific interfaces and implementations. Do not expose `IQueryable`, provider contexts, or `Include` in application-facing contracts.
- For set-wide updates/deletes, use the existing bulk or set-based operations where appropriate; do not load every entity merely to change the same property.
- Use `Guard` for domain invariants and follow the existing localized error flow.
- Read [DddBuildingBlocks.md](../../docs/DddBuildingBlocks.md) and [EntityConcepts.md](../../docs/EntityConcepts.md) before changing entity, repository, audit, soft-delete, or projection conventions.

Relevant decisions: ADR-0005, ADR-0036–0042, ADR-0045–0048, ADR-0073.
