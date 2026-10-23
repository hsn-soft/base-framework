# Detector: check your work before you open a pull request

The CI runs on the pull request, so a problem costs you a round trip: push, wait, read the log, fix, push again. The detector runs the
**same checks on your machine** first and shows where it would break.

| File | For |
| --- | --- |
| `detector_mac.sh` | macOS, Linux (bash) |
| `detector_win.ps1` | Windows (PowerShell 7; also works with `pwsh` on macOS/Linux) |

## How to run

```bash
# macOS / Linux
./scripts/detector/detector_mac.sh
./scripts/detector/detector_mac.sh --skip-integration    # no Docker, faster, not the full check
```

```powershell
# Windows
./scripts/detector/detector_win.ps1
./scripts/detector/detector_win.ps1 -SkipIntegration
```

A full run takes about two minutes (most of it the integration tests). You need the .NET SDK, git, python3 (`python` on Windows) and
Docker for the integration tests. On Windows `bash` must be on the path too (Git for Windows installs it); the repository's test
scripts are bash scripts. The detector checks all of this first and says what is missing.

## What it does

The stages are the ones of the CI workflow (`.github/workflows/net-core-build-and-test.yml`), in the same order:

| Stage | What it checks | The same in the CI |
| --- | --- | --- |
| Restore | the packages can be restored | `Restore Dependencies` |
| Every package has a test project | each package under `src/` has a `test/<Package>.Tests` | `Every Package Has Unit Tests` |
| Format (C#) | `dotnet format` whitespace and style rules find nothing to change | `Format Check (C#)` |
| Format (JSON) | the JSON files are formatted | `Format Check (JSON)` |
| Build | `Release`, a clean build, **warnings are errors** | `Build Solution` |
| Unit tests | every `*.Tests` project, with coverage | `Unit Tests With Coverage` |
| Integration tests | `*.IntegrationTests`, with coverage (Docker, Testcontainers) | `Integration Tests With Coverage` |
| Coverage gate | unit and integration coverage merged; line coverage of `src` at or above the threshold | `Coverage Gate JOB` |

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
| Format (C#) | `dotnet format whitespace HsnSoft.Base.sln` and `dotnet format style HsnSoft.Base.sln --severity info` (the pre-commit hook does it for staged files). A naming rule (IDE1006) has to be fixed by hand. |
| Format (JSON) | `dotnet run scripts/format-json.cs -- --all` |
| Build | read the errors in the log; a warning is an error here, as in the CI |
| Unit / integration tests | run the failing project alone: `dotnet test test/<Project>` |
| Coverage gate | the log lists the lines nobody ran (`not covered: file:line`); add a test |
| Every package has a test project | add `test/<Package>.Tests` |

Like the CI, the test scripts stop at the first failing test project, so the counts of a failed run only cover the projects that ran.

## When the CI changes

The detector is a copy of the CI's steps, not the CI itself (the CI runs its jobs in parallel). When someone adds a step to
`net-core-build-and-test.yml`, the same stage goes into **both** detector scripts, and the table above gets a row.
