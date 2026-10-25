# ADR-0043: The order of the references in a project file

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The project files of the framework, of the shared projects and of the services listed their references in the order they were added: a package here, a framework project there, the service's own project between them. Reading what a project depends on, and seeing at once whether a service reaches
into the framework, into the shared projects or into another service, meant reading every line.

## Decision

- **The references of a project go in groups, one `<ItemGroup>` for each kind, from the top down: external, framework, shared, own.**
  1. **external:** `FrameworkReference` and the `PackageReference`s that are not ours (`Microsoft.*`, `Yarp.ReverseProxy`, `Newtonsoft.Json`, ...);
  2. **framework:** `HsnSoft.Base.*` (a `ProjectReference` to `src/` here, a `PackageReference` in a product repository);
  3. **shared:** `<Product>.Shared.*` (`Abc.Shared.Hosting`, `Hhs.Shared.Helper`);
  4. **own:** everything else, the other projects of the service or of the product (`Abc.IdentityService.Domain`).
- **Inside a group the items are in alphabetical order** of the name of the package or of the project (a `FrameworkReference` first). A comment that stands above an item stays with it.
- **Groups of files come before the references:** `Content`, `EmbeddedResource`, `None`, `Using` keep their own groups above the reference groups. A project with nothing of a kind has no group of it.
- **It is checked, not remembered:** `scripts/ci-check-csproj-order.py` finds a project that breaks the rule (the CI runs it, and the detector as `Project file reference order`) and `--fix` rewrites the project files into it.

## Consequences

- A project file reads the same everywhere: the first group is what the world gives, the last is the product's own.
- A new reference goes into the group of its kind, in its place; a project that gets one in the wrong place fails the check with the command that fixes it.

## Related

[ADR-0030](0030-microservice-project-pattern.md), [ADR-0028](0028-shared-hosting-projects.md), [CONTRIBUTING.md](../../CONTRIBUTING.md).
