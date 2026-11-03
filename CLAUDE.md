# Working in this repository

HsnSoft Base Framework is a .NET 10 repository of small, single-capability NuGet packages and their tests. The package/dependency map is [docs/Overview.md](docs/Overview.md); the documentation map is [docs/README.md](docs/README.md).

## Load only task-relevant context

Repository rules distilled from the ADRs live in path-scoped files under [`.claude/rules/`](.claude/rules/). Claude Code loads a rule when its `paths` match files in the task. Follow the matching rule, then consult only its cited ADRs when a decision needs more context. Do not read every ADR for an unrelated task.

Use the short [ADR guide](docs/adr/README.md) to locate decisions and the [full ADR catalog](docs/adr/INDEX.md) only when searching beyond the matching rule. For feature behavior, consult the matching reference document in [docs/README.md](docs/README.md). Use [CONTRIBUTING.md](CONTRIBUTING.md) for contributor workflow and the [CI detector guide](scripts/ci-detector/README.md) for validation commands.

For multi-step work, use the matching on-demand skill under [`.claude/skills/`](.claude/skills/): `add-package`, `add-provider`, `create-tests`, `create-adr`, or `release`. Skills guide the workflow; the path-scoped rules remain the source of standing constraints.

When performing Git operations (commit, push, or opening a PR), follow [CONTRIBUTING.md](CONTRIBUTING.md) and explicitly consult [the Git/release rule](.claude/rules/git-release.md); Git operations do not necessarily touch a file path that activates that rule automatically.

## Repository rules

- Claude works on a `feat/...` branch of its own, cut from `dev`; it may commit and push to that branch, and it never opens a pull request into `dev` or a release branch, never merges into `dev` or `rel/*`, and never pushes to them: the maintainer does (ADR-0132). Do not stack branches. When the maintainer says a push stays with them, commit and do not push.
- Record each new architecture decision in the same change: copy [the ADR template](docs/adr/0000-template.md) and add a row to [the full catalog](docs/adr/INDEX.md). Never rewrite a decision; supersede it with a new ADR.
- Run `dotnet format` and `./scripts/ci-detector/ci_detector_mac.sh` before calling a change done.
- Add new projects to the matching `src` or `test` solution folder; read the solution first (ADR-0006).
- Keep project references in the groups and order in ADR-0043. Every `src/` project reaches Core and must not repeat a sibling-transitive reference (ADR-0075).
- Write documents and code comments in English; the maintainer talks Turkish in conversation, so answer in Turkish.
- Work in `samples/` follows `samples/CLAUDE.md`; decisions about a sample are `samples/docs/adr/` (their own numbering, not the catalog here). The current large effort there is `samples/hsx`, the rebuild of hhs. Never store real credentials or connection strings; use the environment or an ignored `.env`.
