# ADR-0075: The rules of the project files of the framework

- **Status:** Accepted (a living list: a new rule about the references of a project is added to **Rules** below, with its date, instead of a new ADR)
- **Date:** 2026-10-27 (collects [ADR-0004](0004-core-once-and-the-dependency-graph.md) and [ADR-0043](0043-project-file-reference-order.md) and makes the first one a check)

## Context

The rule of ADR-0004 (every package depends on `Core`, directly or through another one, and does not add it twice) was in prose only, and it had been broken in both ways: `WebApi`, `Swashbuckle` and `SwaggerGateway` listed `Core` beside `AspNetCore`, which brings it, and the new `MultiTenancy` package
did not reach `Core` at all. The order of the references (ADR-0043) is already a check. The rules of the project files belong together, in one place that grows.

## Rules

1. **Every package of `src/` except `Core` has at least one project reference and reaches `HsnSoft.Base.Core`**, directly or through another framework package (2026-10-27). `Core` is the root of the graph; a package with nothing else to build on references it.
2. **A package with more than one reference does not list `Core`** (2026-10-27): a sibling it references brings `Core` already, and listing it again only hides what the package depends on.
   **A package with exactly one reference** references `Core` itself, or a project that reaches it (`EventBus.RabbitMQ` has `EventBus`, not `Core`).
   More generally a package does not list a sibling that another one it lists already brings.
3. **The references are in groups, from the top down: external (`FrameworkReference`, NuGet packages that are not ours), framework (`HsnSoft.Base.*`), shared (`<Product>.Shared.*`), own (the other projects of the service or product); each group in alphabetical order** (2026-10-27, as [ADR-0043](0043-project-file-reference-order.md)
   decided): what the world gives is at the top, what is the product's own at the bottom.

## Checks

- Rules 1 and 2: `scripts/ci-detector/ci-force-framework-references-rules.py`, which breaks the CI build (the step `Framework Project References`) and the detector (`Framework project references`) the way a missing test project does; it names the project, the rule and the reference to fix. It was run against projects that break each rule and fails on every one.
- Rule 3: `scripts/ci-detector/ci-force-csproj-order.py` (`--fix` rewrites the files).

## Consequences

- A broken rule fails the build with the line to change; nobody has to remember it.
- A new rule is a line in **Rules** and, when it can be checked, a script that the CI and the detector run.

## Related

[ADR-0002](0002-one-package-one-capability.md), [ADR-0004](0004-core-once-and-the-dependency-graph.md), [ADR-0043](0043-project-file-reference-order.md), [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md).
