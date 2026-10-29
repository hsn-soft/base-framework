# ADR-0073: Auditing and Data are packages of their own

- **Status:** Accepted
- **Date:** 2026-10-27 (changes where [ADR-0037](0037-audit-soft-delete-and-tenants.md) put the audit contracts and the data filter; follows [ADR-0002](0002-one-package-one-capability.md) and [ADR-0004](0004-core-once-and-the-dependency-graph.md))

## Context

The previous framework had a `Auditing` project: read-only contracts (`ICreationAuditedObject`, `IAuditedObject`, `IFullAuditedObject`, ...) that **both** an entity (`Ddd.Domain`) and its DTO (`Ddd.Application`) implemented, and the setter that fills them. The rewrite put what it kept of audit into `Core`
(`ICreationAudited`, `IModificationAudited`, `ISoftDelete`, the setter) and `IDataFilter` there too. The audit interfaces had a mutator (`MarkCreated`) so a DTO could not implement them: an entity and its DTO shared the names of the fields and nothing else, there was no contract for "anything with an audit", and
`Core` grew with a concern of its own.

## Decision

- **`HsnSoft.Base.Auditing`** (on `Core`) holds the audit contracts, in the names of the previous framework:
  - the shapes, read only: `IHasCreationTime` (`CreatedAt`), `IMayHaveCreator` (`CreatedBy`), `ICreationAuditedObject`; `IHasModificationTime` (`UpdatedAt`), `IModificationAuditedObject` (`UpdatedBy`); `IHasDeletionTime : ISoftDelete` (`DeletedAt`), `IDeletionAuditedObject` (`DeletedBy`);
    and the composites `IAuditedObject` (creation and modification) and `IFullAuditedObject` (all). The property names are the ones the entities, the tables and the API already have; only the interfaces were renamed.
  - the marks, for the entities only: `IMarkCreated`, `IMarkModified`, `IMarkDeleted` (the `Mark...` methods the persistence calls). A DTO has nothing to mark.
  - `IAuditPropertySetter`, `AuditPropertySetter`, `AuditStamp` and `AddBaseAuditing()`.
- **`HsnSoft.Base.Data`** (on `Core`) holds `IDataFilter` and `DataFilter` (the switch of the soft delete and tenant filters that follows the flow of the code) and `AddBaseDataFilter()`. It is where what every data store shares will go (seeding, connection names), as in the previous framework.
- **`ISoftDelete` and `IMultiTenant` stay in `Core`**; `ISoftDelete` is now only `IsDeleted` (the marker the filters key on), `IHasDeletionTime` adds `DeletedAt` on top of it.
- **`Ddd.Domain`** is on `Auditing` and `Data`: its entity bases implement the shapes and the marks (`CreationAuditedEntity` is `ICreationAuditedObject` and `IMarkCreated`, `FullAuditedEntity` is `IFullAuditedObject` and `IMarkDeleted`), and it registers the concepts of an entity, `AddBaseEntityConcepts()` (the setter, the filter
  and the current tenant), which the EF Core and MongoDB providers call; the call moved from `Core`. **`Ddd.Application`** gets them through `Ddd.Domain`: its DTO bases (`CreationAuditedEntityDto<TKey>`, `AuditedEntityDto<TKey>`, `FullAuditedEntityDto<TKey>`) implement the same shapes as the entity.
- A package does not list a sibling that another one it references already brings (ADR-0004): `EntityFrameworkCore` and `MongoDB` are on `Ddd.Domain` only, `Ddd.Domain` is on `Auditing` and `Data` only.

## Consequences

- An entity and its DTO share the audit contract, so one rule (a sort, a filter, a screen) reads the audit fields of either (tested: one function reads the `CreatedAt` and `CreatedBy` of an entity and of its DTO).
- `Core` has no audit and no data filter; a product that wants neither does not get them. The graph of ADR-0004 has two more rows.
- `ICreationAudited` and `IModificationAudited` are gone; an entity that implemented them implements the shapes and the marks (the base classes do it for the ones that derive from them).
- The tests of the setter, of the filter and of the registration of the concepts moved with the code (`Auditing.Tests`, `Data.Tests`, `Ddd.Domain.Tests`).

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0037](0037-audit-soft-delete-and-tenants.md), [ADR-0042](0042-ddd-domain-and-application-packages.md).
