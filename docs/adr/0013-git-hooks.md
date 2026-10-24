# ADR-0013: Git hooks: format on commit, build and unit tests on push

- **Status:** Accepted
- **Date:** 2026-10-22

## Context

Formatting problems and warnings reached the pull request and failed CI; each cost a round trip. A hook that is slow is skipped, a hook that is bypassed protects nothing, and a hook that
needs manual setup is not installed.

## Decision

Hooks are managed with **Husky.Net** (`.husky/`, tool pinned in `.config/dotnet-tools.json`); the first build or restore installs them (`dotnet tool restore` + `dotnet husky install`).

- **Pre-commit** (`task-runner.json`, group `pre-commit`, only on the staged files): `dotnet format whitespace` and `dotnet format style --severity info` on the staged `.cs` files, `scripts/format-json.cs` on the
  staged `.json` files (not `.vscode/`), then `git add` of what was formatted, then a final `dotnet format style --verify-no-changes` that **refuses the commit** if a rule that cannot be fixed automatically
  (a naming rule, for example) is still broken. It adds a few seconds to a commit.
- **Pre-push** (group `pre-push`): a **Release build with warnings as errors** (`-p:TreatWarningsAsErrors=true`, as CI) and the **unit tests**; the push is refused if either fails, so a broken branch never reaches
  the remote. About 30 seconds. **The integration tests are not in it** (they need Docker and take longer): the detector and CI run them.
- **`HUSKY=0`** skips a hook in an emergency (`HUSKY=0 git push`); CI runs the same checks and fails the pull request anyway, so the hook is a convenience, never the only guard.
- A hook never commits or pushes by itself and never changes anything except formatting of the staged files.

## Consequences

- A formatter run by the hook can change a file after `git add`; the hook stages the result, so what is committed is what was formatted.
- A commit made in a working tree with unrelated unstaged edits only formats the staged files, so those edits are untouched.
- Skipping the hooks shows up in CI as a format or build failure.

## Related

[ADR-0008](0008-code-style-and-analyzers.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), `.husky/task-runner.json`, `CONTRIBUTING.md` (Code style and formatting).
