# ADR-0072: A host chooses its localization and its cache

- **Status:** Accepted
- **Date:** 2026-10-27 (changes [ADR-0028](0028-shared-hosting-projects.md), [ADR-0034](0034-redis-and-rabbitmq-in-the-abc-hosts.md) and the way a host asks for [ADR-0068](0068-the-shared-texts-of-a-product-are-a-project-of-their-own.md))

## Context

`Abc.Shared.Hosting` registered the localization (`AddBaseLocalization`, `UseBaseLocalization` in the pipeline) and offered Redis (`AddAbcRedis`), and `AddAbcMicroservice` added both for every microservice. A host that has nothing to say in several languages, or no use for a cache, still got them and
the projects that carry them (`HsnSoft.Base.Localization`, `HsnSoft.Base.Caching.StackExchangeRedis`) in its output. A shared hosting should be usable by a host without either.

## Decision

- **The hosting projects register neither the localization nor Redis.** `Abc.Shared.Hosting` references neither `HsnSoft.Base.Localization` nor `HsnSoft.Base.Caching.StackExchangeRedis`; `AddAbcHosting` / `UseAbcHosting` do not add the localization or its middleware; `AddAbcRedis` is gone;
  `AddAbcMicroservice(configuration, title)` is the shell only (no `localizedAssemblies`).
- **A host that wants them says so in its `Program.cs`.** A microservice and the auth server make one localization call with the layers outermost first, `AddBaseLocalization(configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(Product).Assembly))`, and call `app.UseBaseLocalization()`
  right after `UseAbcMicroservice()` (the hosting has read the token by then); the services reach the localization package through the shared texts of their Domain project. A host that wants Redis calls the framework's `AddBaseRedis(configuration)` and references the Redis package.
- **In the Abc sample:** the microservices and the auth server choose both; the commercial gateway chooses Redis; neither gateway has texts of its own, so neither chooses the localization.

## Consequences

- A gateway answers its own errors with the framework's English status texts (it asks for no language); a microservice behind it answers in the language of the request as before (tested live: a wrong password in English, Turkish and Russian; a missing product in Turkish).
- The hosting projects are smaller and a host's project file says what it chose; each microservice repeats the two calls, as it repeats its persistence and event bus.
- `UseBaseLocalization()` runs after the health endpoints are mapped now: they do not need a language.

## Related

[ADR-0028](0028-shared-hosting-projects.md), [ADR-0034](0034-redis-and-rabbitmq-in-the-abc-hosts.md), [ADR-0038](0038-localization.md), [ADR-0068](0068-the-shared-texts-of-a-product-are-a-project-of-their-own.md), [ADR-0069](0069-who-references-which-shared-project-in-abc.md).
