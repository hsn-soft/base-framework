# ADR-0075: The rules of the project files of the framework

- **Status:** Accepted (a living list: a new rule about the references of a project is added to **Rules** below, with its date, instead of a new ADR)
- **Date:** 2026-10-27 (collects [ADR-0004](0004-core-once-and-the-dependency-graph.md) and [ADR-0043](0043-project-file-reference-order.md) and makes the first one a check)

## Context

The rule of ADR-0004 (every package depends on `Core`, directly or through another one, and does not add it twice) was in prose only, and it had been broken in both ways: `WebApi`, `Swashbuckle` and `SwaggerGateway` listed `Core` beside `AspNetCore`, which brings it, and the new `MultiTenancy` package
did not reach `Core` at all. The order of the references (ADR-0043) is already a check. The rules of the project files belong together, in one place that grows.

## Rules

1. **Every package of `src/` reaches `HsnSoft.Base.Core`**, directly or through another framework package (2026-10-27). `Core` is the root of the graph; a package that is a root of its own branch references it.
2. **A package does not list what one of the projects it lists already brings** (2026-10-27): a package that references a sibling does not add `Core`, and it does not list a sibling that one of its other siblings brings. Reaching `Core` is the proof that the reference is there; listing it again only hides what the package depends on.
3. **The references are in groups, from the top down: external (`FrameworkReference`, NuGet packages that are not ours), framework (`HsnSoft.Base.*`), shared (`<Product>.Shared.*`), own (the other projects of the service or product); each group in alphabetical order** (2026-10-27, as [ADR-0043](0043-project-file-reference-order.md)
   decided): what the world gives is at the top, what is the product's own at the bottom.

## Checks

- Rules 1 and 2: `scripts/ci-check-framework-references.py` (the CI runs it, the detector as `Framework project references`); it names the project and the reference to fix.
- Rule 3: `scripts/ci-check-csproj-order.py` (`--fix` rewrites the files).

## Consequences

- A broken rule fails the build with the line to change; nobody has to remember it.
- A new rule is a line in **Rules** and, when it can be checked, a script that the CI and the detector run.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0043](0043-project-file-reference-order.md), [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md).
