---
name: add-package
description: "Use when creating or scaffolding a new HsnSoft.Base NuGet package or capability, including its project file, tests, solution entry, package catalog, and dependency decisions."
---

# Add a framework package

## Procedure

1. Read the matching path-scoped rules in `.claude/rules/architecture.md` and `.claude/rules/dotnet.md`, plus `CONTRIBUTING.md` (Adding a package), ADR-0002–0006 and ADR-0070/0075. Check `docs/Overview.md` and existing projects before designing a new package.
2. Confirm that the capability is product-neutral and independently useful. Choose one capability per package. If it is a technology implementation, verify that a provider-neutral abstraction exists or design it as a separate abstraction package; never combine alternative providers in one package.
3. Decide exactly which package dependencies the implementation and public API require. Every `src/` project must reach `HsnSoft.Base.Core`; do not directly repeat a sibling dependency that already brings Core or another referenced sibling.
4. Create `src/HsnSoft.Base.<Name>/` following a nearby project: import `common.props`, include the package metadata and README, and keep project references in the required alphabetized groups. Put shared package versions in `common.versions.props`, not in the project file.
5. Add `test/HsnSoft.Base.<Name>.Tests/` using `common.test.props`. Add focused unit tests for the behavior and boundary cases; for a provider, also add/update real-service integration tests in `test/HsnSoft.Base.IntegrationTests` using the existing Testcontainers conventions.
6. Add all projects by hand to `HsnSoft.Base.sln` in the matching `src`/`test` solution folder. Add the package and its exact dependencies to `docs/Overview.md`.
7. Record a new architectural decision only if this introduces a lasting rule or chooses between consequential alternatives. If so, use the `create-adr` skill.
8. Restore/build and run the focused unit tests; if this is a provider, run the focused integration tests too. Run the project-reference check/fixer as needed, `dotnet format`, then the full CI detector before calling the change done.

Do not implement an unnecessary dependency merely to make a product or sample convenient. Keep product-specific behavior in that product's shared project or service, not in the framework.
