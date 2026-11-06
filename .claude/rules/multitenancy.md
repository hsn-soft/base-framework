---
paths:
  - "src/HsnSoft.Base.MultiTenancy*/**"
  - "docs/MultiTenancy.md"
  - "docs/EntityConcepts.md"
---

# Multi-tenancy

- Keep tenancy optional and split into its established contracts and integration packages. A service that does not opt into tenancy must not acquire tenant dependencies.
- Resolve the current tenant through the existing `ICurrentTenant`/tenant-source flow; authentication claims and message headers must follow the documented names and semantics.
- Isolate an entity by tenant or by scope (subscription), never both. Preserve the tenant ancestry/path rules and allowed-tenant checks; do not broaden access based only on a tenant identifier supplied by a caller.
- Apply tenant filters and tenant assignment at the established EF Core/MongoDB persistence boundaries. The tenant of a message and of its handler: the event-bus rule.
- Read [MultiTenancy.md](../../docs/MultiTenancy.md) and [EntityConcepts.md](../../docs/EntityConcepts.md) before changing tenant resolution, visibility, filtering, or entity ownership.

Relevant decisions: ADR-0037, ADR-0074, ADR-0128–0129.
