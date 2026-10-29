# ADR-0006: Repository folders and the solution file

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The repository holds the packages, their tests, scripts and documents. We wanted a layout a newcomer, and a new session, can read at a glance, and one that mirrors how products are laid out.

## Decision

```
src/<Package>/                    one project per package (ADR-0002), imports common.props
test/<Package>.Tests/             unit tests of that package (ADR-0009); imports common.test.props
test/HsnSoft.Base.IntegrationTests/   every provider against the real service (ADR-0010)
docs/                             reference documents (what is, how to use) and docs/adr/ (why)
scripts/                          detector, run-tests, coverage gate, sonar, pack-local, graylog-fields
docker/                           local infrastructure (SonarQube compose)
.github/workflows/                the CI and the NuGet deploy (ADR-0012)
.husky/                           the git hooks (ADR-0013)
```

- **The solution file mirrors the folders, and it is kept in step.** `HsnSoft.Base.sln` has the solution folders `src` and `test`. **Whenever a folder or a project is added, the solution file is updated in the same change**, in the matching solution folder
  (`dotnet sln add --solution-folder <folder> <csproj>`), and the solution is built. The maintainer edits the solution by hand; read it before changing it.

## Consequences

- Moving a project one level deeper changes the relative `ProjectReference` paths (`..\..\..\src\...`) and the solution file.

## Related

[ADR-0005](0005-where-code-lives.md), `CONTRIBUTING.md` (Adding a package), `HsnSoft.Base.sln`.
