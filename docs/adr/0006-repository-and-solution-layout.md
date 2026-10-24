# ADR-0006: Repository folders, the solution file and the sample systems

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The repository holds the packages, their tests, example systems that compose them, scripts and documents. As it grew, the samples became several systems (a gateway with services, an agent, a monolith, a
service in the making) and the solution file turned into a flat list. We wanted a layout a newcomer, and a new session, can read at a glance, and one that mirrors how products are laid out.

## Decision

```
src/<Package>/                    one project per package (ADR-0002), imports common.props
test/<Package>.Tests/             unit tests of that package (ADR-0009); imports common.test.props
test/HsnSoft.Base.IntegrationTests/   every provider against the real service (ADR-0010)
samples/<system>/<Project>/       example systems built only from the framework packages
docs/                             reference documents (what is, how to use) and docs/adr/ (why)
scripts/                          detector, run-tests, coverage gate, sonar, pack-local, graylog-fields
docker/                           local infrastructure (SonarQube compose)
.github/workflows/                the CI and the NuGet deploy (ADR-0012)
.husky/                           the git hooks (ADR-0013)
```

- **Samples: one folder per system.** `samples/xyz` (a JWT-secured gateway, an auth server and three services with `Xyz.Shared`), `samples/agent` (`Agt.Scheduler.Host`, a worker),
  `samples/def` (`Def.Signer.Host`, a monolith web api), `samples/abc` (`service/identity/Abc.IdentityService.*`, a service in the making, `gateway/` with the commercial and the public gateway ([ADR-0027](0027-gateways-per-audience.md)), `app/` with what people use (the auth server, `Abc.AuthServer.Host`, and the two MVC web applications, `Abc.CommercialWeb.Host` and `Abc.PublicWeb.Host`, [ADR-0029](0029-one-auth-server-issues-the-token.md)) and `shared/` with `Abc.Shared.Hosting`, `.Gateways` and `.Microservices` ([ADR-0028](0028-shared-hosting-projects.md)); the service is laid out as the production identity service:
  `Domain` with `UserDomain/{Consts,Entities,Exceptions,Repositories}`, `Application` with `Contracts/UserDomain/{Dtos,Services}` and `Services/`, `EntityFrameworkCore` with `Context/`,
  `Configurations/`, `Repositories/`, `Http.Host` with `Controllers/`; the host references Application and EntityFrameworkCore, both reference Domain).
- **Samples are applications, not packages:** they use `samples/Directory.Build.props` (not `common.props`) and `samples/Directory.Packages.props` (central package versions, samples only).
  The framework projects manage their own versions.
- **The solution file mirrors the folders, and it is kept in step.** `HsnSoft.Base.sln` has the solution folders `src`, `test` and `samples` with one solution folder per system under `samples`
  (`abc`, `agent`, `def`, `xyz`). **Whenever a folder or a project is added, the solution file is updated in the same change**, in the matching solution folder
  (`dotnet sln add --solution-folder <folder> <csproj>`), and the solution is built. The maintainer edits the solution by hand; read it before changing it.
- A sample system's scripts live with it (`samples/xyz/run-all.sh`, `smoke-test.sh`); the shared sample documentation is `samples/README.md`.

## Consequences

- Adding a sample system means a folder, a solution folder and a section in `samples/README.md`.
- Moving a project one level deeper changes the relative `ProjectReference` paths (`..\..\..\src\...`) and the solution file; the scripts that start samples must follow.
- The detector and the CI build the samples (the solution) but do not run them (see [ADR-0010](0010-integration-tests-with-testcontainers.md)).

## Related

[ADR-0005](0005-where-code-lives.md), `samples/README.md`, `CONTRIBUTING.md` (Adding a package), `HsnSoft.Base.sln`.
