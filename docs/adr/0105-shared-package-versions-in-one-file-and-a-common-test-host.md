# ADR-0105: The versions of the shared dependencies are in one file that libraries and tests both import, and tests share a small test host

- **Status:** Accepted
- **Date:** 2026-10-29 (changes where the versions of [ADR-0007](0007-build-settings-and-versioning.md) live)

## Context

ADR-0007 put the versions of the shared dependencies in `common.props` "so a framework upgrade is one edit". Tests import `common.test.props`, which did not see those properties, so every test project wrote its own literal `10.0.13`: 72 lines in 31 projects. Moving to
`10.0.14` changed `common.props` only, and the restore of the whole solution failed (NU1605, a test project asks for an older version than the library it tests brings). The same three packages (`Microsoft.Extensions.Configuration`, `DependencyInjection`, `Logging`) were also listed in
almost every test project: the libraries bring only the abstractions, and a test that makes a container, a logger or a configuration needs the implementations.

## Decision

- **`common.versions.props` holds the versions** (`MicrosoftAspNetCorePackageVersion`, `MicrosoftEntityFrameworkCorePackageVersion`) and is imported by both `common.props` and `common.test.props`. A new patch of ASP.NET Core or EF Core is one edit of that file. **No project file writes these versions as a literal**: libraries and tests use the properties.
- **`common.test.props` gives every test project the test host** and nothing else: `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging`. What only some tests need (`Microsoft.AspNetCore.TestHost`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.Extensions.Hosting`, the database and broker clients)
  stays in the project that needs it, with the version property. The common list does not grow for convenience: a package joins it only when most test projects need it.
- The CI cache keys include `common.versions.props`.

## Consequences

- A framework upgrade is one file, for the libraries and the tests alike; a test project cannot fall behind it.
- A test project that does not use configuration still gets the package; it costs nothing at run time and keeps the test projects free of the same three lines.
- `System.Security.Cryptography.Pkcs` of the SqlServer package is pinned to a version of its own on purpose (a transitive correction) and is not a shared version.

## Related

[ADR-0007](0007-build-settings-and-versioning.md), [ADR-0009](0009-unit-tests-every-package.md), [ADR-0070](0070-a-framework-package-brings-what-its-api-exposes.md).
