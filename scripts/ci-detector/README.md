# CI detector: check your work before you open a pull request

The CI runs on the pull request, so a problem costs you a round trip: push, wait, read the log, fix, push again. The detector runs the
**same checks on your machine** first and shows where it would break.

| File | For |
| --- | --- |
| `ci_detector_mac.sh` | macOS, Linux (bash) |
| `ci_detector_win.ps1` | Windows (PowerShell 7; also works with `pwsh` on macOS/Linux) |
| `ci-check-coverage.py` | the coverage gate: merges the coverage reports and fails below the threshold (`merge_lines` is also used by the summary) |
| `ci-force-csproj-order.py` | the order of the references in a project file ([ADR-0043](../../docs/adr/0043-project-file-reference-order.md)); `--fix` puts them in order |
| `ci-force-framework-references-rules.py` | the reference rules of the framework projects ([ADR-0075](../../docs/adr/0075-rules-of-the-framework-project-files.md)) |
| `ci-summary.py` | the page at the end of a CI run: the time of every step, the test counts, the coverage; with `--badges <folder>` also the endpoint files of the README badges ([ADR-0114](../../docs/adr/0114-the-readme-badges-are-made-by-the-ci-and-published-to-their-own-branch.md)) |
| `ci-publish-badges.sh` | publishes those files to the branch `badges` (a scheduled or manual run on `dev` only) |
| `ci-check-test-project-exist.sh` | every package under `src/` has a test project under `test/` |
| `ci-check-referenced-files.sh` | every `scripts/` path a workflow runs exists and is not ignored by `.gitignore` (a `[Rr]elease/` pattern once kept a whole folder out of a commit) |
| `ci-timed.sh` | runs a CI step and records how long it took, for the summary |
| `format-json.cs` | the JSON formatter and checker (see *Format* below) |
| `format-staged.py` | formats the staged files of a commit (see *Format* below) |

Everything the CI and the detector run lives in this folder, so the two cannot drift apart. The one script they share with others stays in `scripts/`:
`../run-tests.sh` runs the test projects (the CI, the detector and the pre-push hook all call it).

## How to run

```bash
# macOS / Linux
./scripts/ci-detector/ci_detector_mac.sh
./scripts/ci-detector/ci_detector_mac.sh --skip-integration    # no Docker, faster, not the full check
```

```powershell
# Windows
./scripts/ci-detector/ci_detector_win.ps1
./scripts/ci-detector/ci_detector_win.ps1 -SkipIntegration
```

A full run takes about two minutes (most of it the integration tests). You need the .NET SDK, git, python3 (`python` on Windows) and
Docker for the integration tests. On Windows `bash` must be on the path too (Git for Windows installs it); the repository's test
scripts are bash scripts. The detector checks all of this first and says what is missing.

## What it does

The stages are the ones of the CI workflow (`.github/workflows/ci-control-workflow.yml`), in the same order:

| Stage | What it checks | The same in the CI |
| --- | --- | --- |
| Clean | the detector starts from a clean tree, as the CI does: `scripts/cleaner/` deletes build output, old test results and editor leftovers (never source, never tracked files); its list is in `logs/clean.log`, the screen shows the count | the CI starts from a clean checkout |
| Every package has a test project | each package under `src/` has a `test/<Package>.Tests` | `Repository Rules` |
| Project file reference order | the references of every project file are in order ([ADR-0043](../../docs/adr/0043-project-file-reference-order.md)) | `Repository Rules` |
| Framework project references | every framework project reaches Core and lists nothing a sibling brings ([ADR-0075](../../docs/adr/0075-rules-of-the-framework-project-files.md)) | `Repository Rules` |
| Release scripts (their tests) | the tests of `scripts/versioning/` (the version of a release) pass | `Repository Rules` |
| Restore | the packages can be restored (once; nothing after it restores again) | `Restore Dependencies` |
| Format (C#) | `dotnet format` whitespace and style rules find nothing to change | `Format Check (C#)` |
| Format (JSON) | the JSON files are formatted | `Format Check (JSON)` |
| Build | `Release`, a clean build, **warnings are errors** (once; the tests run on this build) | `Build Solution` |
| Unit tests | every `*.Tests` project, with coverage | `Unit Tests With Coverage` |
| Integration tests | `*.IntegrationTests`, with coverage (Docker, Testcontainers) | `Integration Tests With Coverage` |
| Coverage gate | unit and integration coverage merged; line coverage of `src` at or above the threshold | `Coverage Gate JOB` |

## The jobs of the CI and the stages of the detector

The CI is a pipeline of jobs ([ADR-0109](../../docs/adr/0109-the-ci-jobs-are-a-pipeline-built-once.md)); the detector is one process on one machine, so it runs the same stages one after the other:

| CI job | Detector stages |
| --- | --- |
| `CI Rules, Format And Build JOB` | the three rule stages, Restore, Format (C#), Format (JSON), Build |
| `CI Check Unit Test JOB` | Unit tests |
| `CI Check Integration Test JOB` | Integration tests |
| `CI Coverage Gate JOB` | Coverage gate |
| `CI Summary JOB` | the table at the end: the time of every stage, the test counts, the coverage |

The CI hands the build from one job to the next in an artifact; the detector simply keeps it in the folder. Neither restores or builds twice.

The screen is cleared when the detector starts (only on a terminal), and the table ends with the **total time**; the start and the end line carry the time of the day.

The threshold is read from the workflow file (`COVERAGE_THRESHOLD`), so it cannot drift from the CI.

A stage that cannot run because an earlier one failed is **SKIPPED**, not shown as passed: a failed restore skips format and build, a
failed build skips the tests, and the coverage gate needs both kinds of tests.

## What you see

First a line per stage as it finishes, then the table:

```
  Stage                                  Result    Time    Detail
  Restore                                PASS      2s
  Every package has a test project       PASS      0s
  Format (C#)                            FAIL      13s     TestResults/detector/logs/format-cs.log
  Build (Release, warnings are errors)   PASS      7s
  Unit tests                             PASS      25s
  Integration tests                      PASS      59s
  Coverage gate (>= 99 %)                PASS      0s
```

- **Everything passed:** the test counts (unit and integration: total, passed, failed, skipped) and the line coverage of `src`:
  unit only, integration only, and merged, which is the number the CI gates. It ends with "Every stage passed. The pull request is
  safe to open."
- **A stage failed:** the lines of its log that say what broke (compiler errors, the failed test with its expected and actual value,
  the files that need formatting) and the path of the full log under `TestResults/detector/logs/`. The exit code is 1.
- **`--skip-integration`:** nothing fails, but the integration stage and the coverage gate show SKIPPED and the closing line says that
  this is not the full check. Run the full check once before the pull request.

The scripts ask dotnet for English messages and read the test counts from the `.trx` result files, so the output is the same on a
machine whose language is not English.

The results are written to `TestResults/detector/` (git ignores it); each run starts clean.

## Fixing what it finds

| Stage | Usual fix |
| --- | --- |
| Project file reference order | the references of every project file are in groups: external, framework, shared, own, each in alphabetical order | `Project File Reference Order` |
| Format (C#) | `dotnet format whitespace HsnSoft.Base.sln` and `dotnet format style HsnSoft.Base.sln --severity info` (the pre-commit hook does it for staged files). A naming rule (IDE1006) has to be fixed by hand. |
| Format (JSON) | `dotnet run scripts/ci-detector/format-json.cs -- --all` |
| Build | read the errors in the log; a warning is an error here, as in the CI |
| Unit / integration tests | run the failing project alone: `dotnet test test/<Project>` |
| Coverage gate | the log lists the lines nobody ran (`not covered: file:line`); add a test |
| Every package has a test project | add `test/<Package>.Tests` |
| Project file reference order | `python3 scripts/ci-detector/ci-force-csproj-order.py --fix` puts the references in order |
| Framework project references | a package of `src/` must reach `Core` and must not list what a sibling brings (ADR-0075): fix the `ProjectReference` the message names |

Like the CI, the test scripts stop at the first failing test project, so the counts of a failed run only cover the projects that ran.

## When the CI changes

The detector is a copy of the CI's steps, not the CI itself (the CI runs its jobs in parallel). When someone adds a step to
`ci-control-workflow.yml`, the same stage goes into **both** detector scripts, and the table above gets a row.

## Format

Two scripts keep the code and the JSON files in the format of the repository ([ADR-0008](../../docs/adr/0008-code-style-and-analyzers.md), [ADR-0013](../../docs/adr/0013-git-hooks.md)):

| Script | What it does | Who runs it |
| --- | --- | --- |
| `format-json.cs` | formats JSON files (a file-based C# program: `dotnet run scripts/ci-detector/format-json.cs -- <files>`); `--all --check` only checks every JSON file of the repository and fails if one is not formatted | the **pre-commit hook** (the staged `.json` files), the **CI** (`Format Check (JSON)`) and the **detector** (`Format (JSON)`) |
| `format-staged.py` | runs `dotnet format` on the staged `.cs` files: it finds the solution of each file (the deepest `.sln` above it), so a file of a nested solution is formatted with that solution; modes `whitespace`, `style` and `verify-style` | the **pre-commit hook** only (`.husky/task-runner.json`); the CI and the detector check the whole solution with `dotnet format ... --verify-no-changes` instead |

So a file is formatted when it is committed, and the whole repository is checked by the CI and by the detector: a commit made without the hook (`--no-verify`) is caught there.
