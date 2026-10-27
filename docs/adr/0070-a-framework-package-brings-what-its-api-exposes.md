# ADR-0070: A framework package brings what its API exposes

- **Status:** Accepted
- **Date:** 2026-10-26 (follows [ADR-0003](0003-abstractions-separate-from-providers.md) and [ADR-0004](0004-core-once-and-the-dependency-graph.md))

## Context

A project of a product that uses a framework type needs the packages that type's signature mentions. The Abc services listed some of them themselves: `Microsoft.EntityFrameworkCore.Relational` in each EF Core project (for `ToTable` and the like), the dependency-injection and localization abstractions in each Application
project, `HsnSoft.Base.EntityFrameworkCore` beside the provider that already references it, `HsnSoft.Base.EventBus` beside the contracts that already bring it. Every service repeated the list, and a new service had to know it.

## Decision

- **A framework package declares the packages its public API exposes**, and a project that references it gets them. `HsnSoft.Base.EntityFrameworkCore` references `Microsoft.EntityFrameworkCore.Relational` (the providers of the framework are relational and its own
  `UpdateWhereAsync` and the inbox use it); `HsnSoft.Base.Ddd.Application` references `Microsoft.Extensions.DependencyInjection.Abstractions` (`AddBasePaging` is an extension of `IServiceCollection`); `EventBus.EntityFrameworkCore` no longer lists the relational package itself.
  The localization abstractions come with `HsnSoft.Base.Localization` (it references them), which a service reaches through the shared texts of its product.
- **A product project references the layer it uses and not what that layer brings**: an EF Core project of a service references its Domain and the provider (`EntityFrameworkCore.PostgreSql`, `EntityFrameworkCore.SqlServer`, and `EventBus.EntityFrameworkCore` for the inbox), which bring the base package and the relational package; an Application project references
  `Ddd.Application`, the contracts and its Domain.

## Consequences

- The project files of the services say what they use, not what their dependencies need (the table is in `samples/abc/REFERENCES.md`).
- The EF Core base package now depends on the relational package: a non-relational EF provider would need its own base. Every provider of the framework (SQL Server, PostgreSQL, and the MySQL and Oracle that may come) is relational, so this costs nothing today.
- An Application project that needs `IStringLocalizer` relies on its Domain referencing the shared texts (optional there) or on referencing the framework's localization package itself.

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0069](0069-who-references-which-shared-project-in-abc.md).
