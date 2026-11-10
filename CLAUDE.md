# Working in this repository

HsnSoft Base Framework is a .NET 11 repository of small, single-capability NuGet packages and their tests. The package/dependency map is [docs/Overview.md](docs/Overview.md); the documentation map is [docs/README.md](docs/README.md).

## Load only task-relevant context

Repository rules distilled from the ADRs live in path-scoped files under [`.claude/rules/`](.claude/rules/). Claude Code loads a rule when its `paths` match files in the task. Follow the matching rule, then consult only its cited ADRs when a decision needs more context. Do not read every ADR for an unrelated task.

Use the short [ADR guide](docs/adr/README.md) to locate decisions and the [full ADR catalog](docs/adr/INDEX.md) only when searching beyond the matching rule. For feature behavior, consult the matching reference document in [docs/README.md](docs/README.md). Use [CONTRIBUTING.md](CONTRIBUTING.md) for contributor workflow and the [CI detector guide](scripts/ci-detector/README.md) for validation commands.

For multi-step work, use the matching on-demand skill under [`.claude/skills/`](.claude/skills/): `add-package`, `add-provider`, `create-tests`, `create-adr`, or `release`. Skills guide the workflow; the path-scoped rules remain the source of standing constraints.

When performing Git operations (commit, push, or opening a PR), follow [CONTRIBUTING.md](CONTRIBUTING.md) and explicitly consult [the Git/release rule](.claude/rules/git-release.md); Git operations do not necessarily touch a file path that activates that rule automatically.

## Definition of done

A change is done when every line below that applies has been done **in the same change**, not later:

1. **Decision.** A new rule other code must follow, or a consequential choice between alternatives, is an ADR (next number, a row in `docs/adr/INDEX.md`); a changed decision is a new ADR that supersedes the old one, which gets a status note. A rule Claude has to keep is also a line in the matching `.claude/rules/*.md` (or a new rule file with its `paths`) that cites the ADR.
2. **Documents.** What the change touches is written where it is owned: the project and dependency map (`docs/Overview.md`), `README.md`, `docs/Checks.md`, `docs/Releasing.md`, `docs/BranchRules.md`, the feature reference in `docs/`. A new script, workflow, endpoint or setting is documented where a developer will look for it; a new setting has its values in every `appsettings.{environment}.json` (never a real secret) and, if it needs a credential, `.env.example`.
3. **Tests.** Unit tests for every new or changed behavior, with the boundary and error cases; integration tests (a real host, Testcontainers) when the change crosses a database, broker, cache or HTTP boundary or composes a host; a test that reads the settings when the settings of several files have to agree. No test depends on time, cores, order or ports: a positive assertion waits for the condition (bounded) and a clock is a `TimeProvider` (see `testing-ci.md`).
4. **Scripts and workflows.** A script or a workflow change keeps the detector and the CI in step (`docs/Checks.md`); a script a person runs by `source` works in bash and in zsh.
5. **Checks.** Run `dotnet format` and the detector (or the check script of the repository) and read the result **from the log file** (a long run writes to a file, never to a pipe that a child process can keep open).
6. **Other repositories.** A rule or a fix that holds in more than one repository (the framework, the shared libraries, the services, the apps, the gateway, the samples) is applied to each of them, or the change says why not.
7. **Report.** Say what was done and what was not (a test not run, a version that waits for a release); commit on the own `feat/` branch and never push to `dev` or `rel/*`.

## Repository rules

- **No agent without the maintainer's approval for that call** (ADR-free working rule, [`.claude/rules/agents.md`](.claude/rules/agents.md)): before an `Agent`, a fork or a `Workflow`, say why, the rough tokens and time, what happens without it and the exact scope, and wait for a yes; one approval never covers the next call.
- Claude works on a `feat/...` branch of its own, cut from `dev`; it may commit and push to that branch, and it never opens a pull request into `dev` or a release branch, never merges into `dev` or `rel/*`, and never pushes to them: the maintainer does (ADR-0132). Do not stack branches. When the maintainer says a push stays with them, commit and do not push.
- What runs at the commit, the push and the pull request, and the scripts that run every check, is the table in [docs/Checks.md](docs/Checks.md); run the detector and `dotnet format` from it before calling a change done.
- Which branch may be merged into which is the table in [docs/BranchRules.md](docs/BranchRules.md) (enforced by `ci-branch-rule.yml`): use it to choose the target of a branch and when a rule changes, update the table, the workflow and the ADR together.
- Record each new architecture decision in the same change: copy [the ADR template](docs/adr/0000-template.md) and add a row to [the full catalog](docs/adr/INDEX.md). Never rewrite a decision; supersede it with a new ADR.
- Run `dotnet format` and `./scripts/ci-detector/ci_detector_mac.sh` before calling a change done.
- Add new projects to the matching `src` or `test` solution folder; read the solution first (ADR-0006).
- Keep project references in the groups and order in ADR-0043. Every `src/` project reaches Core and must not repeat a sibling-transitive reference (ADR-0075).
- Write documents and code comments in English; the maintainer talks Turkish in conversation, so answer in Turkish.
- Work in `samples/` follows `samples/CLAUDE.md`; decisions about a sample are `samples/docs/adr/` (their own numbering, not the catalog here). The current large effort there is `samples/hsx`, the rebuild of hhs. Never store real credentials or connection strings; use the environment or an ignored `.env`.
