# ADR-0157: The framework targets net11.0 and the 2026.11 base is released as a candidate first

- **Status:** Accepted
- **Date:** 2026-11-10

## Context

.NET 11 is released. ADR-0007 fixed `net10.0` as the one target of every library, test and sample project, and the base of the version at `2026.10`.
Two of the packages the framework needs have no stable build for .NET 11 yet: `Npgsql.EntityFrameworkCore.PostgreSQL` (latest `11.0.0-rc.3`, which requires
exactly `Microsoft.EntityFrameworkCore 11.0.0-rc.3.26522.148`) and `MySql.EntityFrameworkCore` (latest `11.0.0-preview`). `Serilog.Extensions.Logging` has no 11.x
build (`10.0.0` is the latest and works on net11.0). The samples reference `src/` projects, so they must target the same framework as the libraries.

With a .NET 11 SDK the build also reports what the .NET 10 SDK did not: NU1510 (a `PackageReference` that the framework already brings, which the build
now asks to remove), and the new analyzer results CA1830, CA2026 (use `JsonElement.Parse`), IDE0005 and IDE0028 (collection expressions, now with `with(...)`).
CI treats warnings as errors, so each of them would fail the build.

## Decision

- Every project (`common.props`, `common.test.props`, `samples/Directory.Build.props`) targets `net11.0`; the workflows install `11.0.x`. Local work needs the .NET 11 SDK.
- `<BaseVersion>` is `2026.11`. The first release of this base is a **candidate** (`2026.11.0-rc.<yyMMdd>.<run>`, ADR-0115); the stable `2026.11.0` is made only when
  the prerelease dependencies below have stable builds and the candidate has been proven in the services.
- The EF Core family (`MicrosoftEntityFrameworkCorePackageVersion`) is `11.0.0-rc.3.26522.148`, because Npgsql `11.0.0-rc.3` needs exactly that version;
  the ASP.NET Core / Extensions family is `11.0.0`. `MySql.EntityFrameworkCore` is `11.0.0-preview`. When a stable build of a dependency appears, the
  version is raised in the same way as any other (one edit in `Directory.Packages.props`) and the stable release follows.
- A `PackageReference` that the build reports as NU1510 is removed, not suppressed; the new analyzer results are fixed in the code, not turned off.

## Consequences

- A service that takes `2026.11.0-rc.*` takes the prerelease EF Core and Npgsql with it and must target `net11.0`.
- The stable `2026.11.0` waits for Npgsql and MySql stable builds; the candidate is a real, usable release in the meantime.
- Code formatted by the .NET 11 SDK may use C# 15 syntax (`with(...)` in collection expressions); older SDKs cannot build the repository.

## Related

ADR-0007 (build settings; its target framework is superseded here), ADR-0115 (versions), ADR-0134 (central package versions), `docs/Releasing.md`, `common.props`, `Directory.Packages.props`.
