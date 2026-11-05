# ADR-0134: Package versions are managed centrally in `Directory.Packages.props`, and a library awaits with `ConfigureAwait(false)` by rule

- **Status:** Accepted
- **Date:** 2026-11-03 (supersedes [ADR-0105](0105-shared-package-versions-in-one-file-and-a-common-test-host.md) where the versions live; the test host part of ADR-0105 stands)

## Context

[ADR-0105](0105-shared-package-versions-in-one-file-and-a-common-test-host.md) put the versions of the two Microsoft families that move together (ASP.NET Core and EF Core) in `common.versions.props`; the versions of every other package (91 references) were written in the project file that uses it, so a new patch of Npgsql, RabbitMQ.Client or Testcontainers was an edit in as many places as there are users. The samples already use central package management (`samples/Directory.Packages.props`). The old framework forced `ConfigureAwait(false)` into every project with the Fody weaver; the new one writes the call by hand (307 of them) and nothing stopped a missing one: the rule was a convention, and the analysis of the old framework found it unenforced ([the analysis](../old-framework/5-tests-repo-graph.md)).

## Decision

- **One file holds the versions.** `Directory.Packages.props` at the root turns on `ManagePackageVersionsCentrally` and holds a `PackageVersion` for every package of the projects under `src/` and `test/` (the test stack of `common.test.props` included). The two properties `MicrosoftAspNetCorePackageVersion` and `MicrosoftEntityFrameworkCorePackageVersion` live in it, so a new patch of either family is one edit. A `PackageReference` in a project file has **no `Version`**; a project that must differ says `VersionOverride` and the reason. `common.versions.props` is removed. The samples keep their own `samples/Directory.Packages.props` (the nearest file wins) and are not affected.
- **A library awaits with `ConfigureAwait(false)`, and the analyzer says so.** `.editorconfig` sets CA2007 to `error` for `src/**/*.cs`. Applications (the samples) and tests are not libraries and are not covered.

## Consequences

A version moves in one place; a new package is two edits (the `PackageVersion`, the reference). The CI cache keys and the sample Docker contexts name `Directory.Packages.props` instead of `common.versions.props`. A missing `ConfigureAwait(false)` in a library now fails the build instead of waiting for a reviewer. A transitive vulnerability floor (a direct reference raised to a safe version) still needs the package as a `PackageReference` of the project that exposes it.

## Related

[ADR-0007](0007-build-settings-and-versioning.md), [ADR-0105](0105-shared-package-versions-in-one-file-and-a-common-test-host.md), [ADR-0075](0075-rules-of-the-framework-project-files.md) (references), `Directory.Packages.props`, `.editorconfig`.
