# ADR-0166: NU5104 stays a warning, so a stable package may depend on a pre-release package until its stable version exists

- **Status:** Accepted (extends [ADR-0007](0007-build-settings-and-versioning.md): one more code in the list of warnings kept as warnings)
- **Date:** 2026-11-21

## Context

The CI builds with warnings as errors ([ADR-0007](0007-build-settings-and-versioning.md)); `CS0618`, `CS0612` and `NU1901`-`NU1904` are kept as warnings because they appear without a code change (an upgrade, a new advisory). `HsnSoft.Base.EntityFrameworkCore.MySql` depends on `MySql.EntityFrameworkCore 11.0.0-preview`, because Oracle has no stable provider for EF Core 11. Packing a **stable** version (`rel/nuget-regular`, `2026.11.x`) of a package with a pre-release dependency raises `NU5104` ("a stable release of a package should not have a prerelease dependency"), and the error stopped the release. A candidate (`2026.11.x-rc.…`) is itself a pre-release, so it never showed the warning.

## Decision

- `NU5104` is added to `WarningsNotAsErrors` in `common.props`, with the reason in the comment next to the others: it stays visible in the log but does not fail the build or the pack.
- The cause is outside the repository (the stable version of the dependency does not exist), and it is not hidden: the dependency stays pinned in `Directory.Packages.props` and is moved to the stable version when Oracle publishes it.
- A package whose own version must be stable is not blocked by a pre-release dependency of another package of the repository; a consumer that does not use MySql never restores the pre-release.

## Consequences

The stable release is no longer stopped by a dependency that the repository cannot change. The price: a stable package can bring a pre-release package to a consumer that uses it (here only the MySql package). When a stable `MySql.EntityFrameworkCore` 11 exists, update `Directory.Packages.props` and the warning disappears.

## Related

[ADR-0007](0007-build-settings-and-versioning.md).
