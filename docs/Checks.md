# What runs when, and how to run it yourself

For developers and for Claude. Three places check your work, each a little later and a little wider: **the commit**, **the push** and **the pull request**. The same checks can be run by hand, so a problem is found on your machine, not after a round trip to the CI.

## One-time setup

```bash
dotnet tool restore        # Husky.Net, pinned in .config/dotnet-tools.json
dotnet husky install       # puts the git hooks in place (.husky/pre-commit, .husky/pre-push)
```
You need the .NET 11 SDK, git, python3 and bash; Docker for the integration tests. The detector checks this and says what is missing.

## What runs when

| Step | Where | What runs | Time | Run it by hand |
|---|---|---|---|---|
| **Before a commit** (pre-commit hook) | your machine, on the **staged** files only | formats the staged C# (whitespace, then style) and JSON, adds the formatted files back to the commit, and fails the commit if a style rule is still broken | seconds | `dotnet husky run --group pre-commit` |
| **Before a push** (pre-push hook) | your machine | the repository rules (every package has a test project, the referenced files exist, the references of the project files are in order and follow the rules: `check-test-projects`, `check-referenced-files`, `check-csproj-order`, `check-project-references`), then a `Release` build of the solution with **warnings as errors**, then **all the unit tests** | about a minute | `dotnet husky run --group pre-push` |
| **Pull request** into `dev` | GitHub Actions | the branch rule, and the whole pipeline below | several minutes | `./scripts/ci-detector/ci_detector_mac.sh` |
| Pull request into a release branch | GitHub Actions | the branch rule only (the pipeline already ran on the pull request into `dev`) | seconds | read [Branch rules](BranchRules.md) |
| **Push to `rel/nuget-candidate` or `rel/nuget-regular`** | GitHub Actions | the publishing to NuGet (`deploy-global-nuget.yml`: build, unit tests, pack, push); the pipeline does not run | several minutes | - |
| By hand (*Run workflow* on `dev`) | GitHub Actions | the pipeline on `dev`: it catches what changes with no push and feeds the README badges | several minutes | - |

`HUSKY=0` skips the hooks. It is an emergency bypass and it does **not** count as a check: the pull request runs the same rules again.

### What the pipeline of the CI is made of

One job after the other (a failure stops the ones after it); the first job builds once and the next ones reuse it. Only **`CI Summary JOB`** and **`CI Branch Rule JOB`** are required checks of a branch.

| Job | What it does | The same on your machine |
|---|---|---|
| `CI Branch Rule JOB` | the source branch of the pull request and where it may go ([Branch rules](BranchRules.md)) | read the table |
| `CI Rules, Format And Build JOB` | the repository rules (every package has a unit test project, the references of the project files in order and without a cycle, the tests of the versioning scripts), restore, format check (C# and JSON), a `Release` build with warnings as errors | detector stages 1-8 |
| `CI Check Unit Test JOB` | every `*.Tests` project, with coverage | detector: *Unit tests* |
| `CI Check Integration Test JOB` | every `*.IntegrationTests` project against real services in Docker, with coverage | detector: *Integration tests* |
| `CI Coverage Gate JOB` | unit and integration coverage merged; **line coverage of `src` at least 99 %** | detector: *Coverage gate* |
| `CI Summary JOB` | one page: the time of every step, the test counts, the coverage; **fails if any job did not succeed** | the table at the end of the detector |

## The scripts, and when to use each

| I want to... | Run |
|---|---|
| **check everything before I open a pull request** (the whole pipeline on my machine) | `./scripts/ci-detector/ci_detector_mac.sh` (Windows: `./scripts/ci-detector/ci_detector_win.ps1`) |
| the same without Docker, faster (**not** the final check; the coverage gate is skipped) | `./scripts/ci-detector/ci_detector_mac.sh --skip-integration` (`-SkipIntegration` on Windows) |
| fix the formatting | `dotnet format HsnSoft.Base.sln` (then the detector); JSON: `dotnet run scripts/ci-detector/format-json.cs -- --all` (add `--check` to only check) |
| run the unit tests | `./scripts/run-tests.sh unit` (`--filter "FullyQualifiedName~Name"` to select; `--configuration Release --no-build` after a build) |
| run the integration tests | `./scripts/run-tests.sh integration` (Docker must run) |
| put the references of a project file in order | `python3 scripts/ci-detector/ci-force-csproj-order.py --fix` |
| check the root namespaces of the hosts | `python3 scripts/ci-detector/ci-check-host-namespaces.py` |
| check the references between projects | `python3 scripts/ci-detector/ci-force-project-references-rules.py` |
| test the versioning scripts | `python3 -m unittest discover -s scripts/versioning -p "test_*.py"` |
| clean the build output (a branch switch, a strange build) | `./scripts/cleaner/cleaner_mac.sh --dry-run`, then without `--dry-run` |
| look at the code quality dashboard (not a gate) | `docker compose -f docker/sonarqube.compose.yml up -d`, then `./scripts/sonar/analysis_mac.sh` ([the guide](../scripts/sonar/README.md)) |

The stages, the options and the logs of the detector are in [the detector guide](../scripts/ci-detector/README.md). When a stage fails, its log is in `TestResults/detector/logs/` (for example `format-cs.log`, `build.log`, `coverage.log`, which lists the lines that no test ran).

## Before you open a pull request

1. `dotnet format HsnSoft.Base.sln`
2. `./scripts/ci-detector/ci_detector_mac.sh` and wait for **"Every stage passed"**
3. Check the source and the target of the pull request in the [branch rules](BranchRules.md)

The reasons are in [ADR-0109](adr/0109-the-ci-jobs-are-a-pipeline-built-once.md), [ADR-0113](adr/0113-the-ci-runs-on-pull-requests-weekly-on-dev-and-by-hand.md), [ADR-0116](adr/0116-the-release-branches-run-the-ci-on-a-push-and-have-their-own-badges.md) and [ADR-0144](adr/0144-the-source-of-every-pull-request-has-a-known-branch-name.md) and [ADR-0152](adr/0152-the-ci-has-no-weekly-run.md) and [ADR-0153](adr/0153-the-ci-does-not-run-on-the-release-branches.md).
