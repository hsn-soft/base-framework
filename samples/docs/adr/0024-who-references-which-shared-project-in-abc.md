# ADR-0069: Who references which shared project in Abc

- **Status:** Accepted
- **Date:** 2026-10-26 (completes [ADR-0028](0028-shared-hosting-projects.md) and [ADR-0068](0068-the-shared-texts-of-a-product-are-a-project-of-their-own.md))
- **Note:** [ADR-0072](0072-a-host-chooses-its-localization-and-its-cache.md): a host references the cache package it chooses (`Caching.StackExchangeRedis`), so "nothing of the framework directly" holds for everything but that choice.

## Context

Abc has shared projects for different things (constants, event contracts, texts, the composition of a host), and several kinds of project that use them: the layers of a service, its host, the auth server, the gateways. Without a rule a project takes whatever is nearest: a domain reaches a hosting project for a constant,
or a gateway carries texts it never says.

## Decision

- **A shared project has one concern and builds on the framework only:** `Abc.Shared.Helper` (constants and small helpers: `AbcTenants`) on `HsnSoft.Base.Core`; `Abc.Shared.Contracts` (the integration events) on `EventBus`; `Abc.Shared.Localization` (the shared texts) on `Localization`; `Abc.Shared.Hosting` (the composition of a host) on the framework projects
  it composes and the logger the hosts use (`Logging.Serilog`, so a host takes it through the hosting and does not reference it); `Abc.Shared.Hosting.Microservices` and `Abc.Shared.Hosting.Gateways` on `Abc.Shared.Hosting`. The hosting projects reference none of Helper, Contracts and Localization: a host asks for what it wants.
- **A microservice takes what each layer needs** (the table of what is required and what is optional is in `samples/abc/REFERENCES.md`; an Application project has no external package of its own, [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md)). Its **Domain** project references `Abc.Shared.Helper` and `Abc.Shared.Localization` (its own `Localization` is the layer after the shared one); its **Application** project references `Abc.Shared.Contracts` (the events it publishes and handles) and registers the paging limits of its lists with the framework's `Ddd.Application` (`AddXApplication(configuration)`), which the hosting does not know; its **Host** references
  `Abc.Shared.Hosting.Microservices` and nothing of the framework directly. The persistence projects (EF Core, MongoDB) reference only their Domain project and the framework.
- **A host that is not a service adds for itself.** The auth server references `Hosting.Microservices`, `Helper` (the tenants) and `Localization` (the text of a wrong password); the gateways reference `Hosting.Gateways` only; the web apps, which only call the gateways, reference no shared Abc project.
- The table in `samples/README.md` and the trees in `samples/abc/REFERENCES.md` are read from the project files; a new reference that breaks the rule shows in review there.

## Consequences

- A project's references say what it is: a Domain that references Hosting would be wrong at a glance.
- `AbcTenants` moved from `Abc.Shared.Hosting` to `Abc.Shared.Helper` (namespace `Abc.Shared.Helper`), so the domain and the persistence do not need a hosting project for a constant. Hosts get it through their Domain project, the auth server references it itself.
- The identity Application project references `Abc.Shared.Contracts` like the others although it publishes no event yet.
- `Abc.Shared.Hosting` lost `Ddd.Application` (it was there only for `AddBasePaging`, which every host got, gateways too) and gained `Logging.Serilog` (the eight hosts that used the shared hosting referenced it each; the two web apps, which do not use it, still do).

## Related

[ADR-0028](0028-shared-hosting-projects.md), [ADR-0068](0068-the-shared-texts-of-a-product-are-a-project-of-their-own.md), [ADR-0003](0003-abstractions-separate-from-providers.md).
