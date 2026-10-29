# ADR-0002: One package, one capability

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

The previous framework shipped a few large packages. A service that only reads MongoDB still loaded JWT, Swagger, a broker and a cache, with their
versions, their vulnerabilities and their startup cost. Products could not take less than the whole. The rewrite had to make "take only what you use" real.

## Decision

- Each package under `src/` is **one capability** (event bus core, the RabbitMQ broker, the Redis cache, the PostgreSQL context, Swagger, ...). An application references
  only the capabilities it uses.
- **A package must not pull in anything its users do not need.** No dependency "because it is convenient".
- **Do not split without a reason.** A package is added only when a real application would use it without the rest. Code that only one product needs is not a framework
  package: it belongs to that product's own shared project (see [ADR-0005](0005-where-code-lives.md)).
- The package list and what each one depends on are in the root [README](../../README.md); a new package gets a row there.

## Consequences

- More, smaller packages: more projects in the solution, more test projects (see [ADR-0009](0009-unit-tests-every-package.md)), more versions to keep consistent
  (they share one version, see [ADR-0007](0007-build-settings-and-versioning.md)).
- A product composes the packages it wants; the framework offers building blocks and never forces a broker, a database or a cache.
- Splitting a package later is a breaking change for everyone who referenced it, so the bar for a new package is "a real application would use it alone".

## Related

[ADR-0003](0003-abstractions-separate-from-providers.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), `CONTRIBUTING.md` (Ground rules, Adding a package).
