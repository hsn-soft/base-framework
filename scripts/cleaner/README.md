# Cleaning the working folder

Two scripts that do the same thing, one per platform. They delete what a build, a test run or your editor leaves behind, so you
can start from a clean tree (after a branch switch, when a build behaves strangely, before zipping the folder).

| File | For |
|---|---|
| `clean_mac.sh` | macOS, Linux (bash) |
| `clean_win.ps1` | Windows (PowerShell 7; also works with `pwsh` on macOS/Linux) |

Your source code is never touched: the scripts only delete the names listed below, never look inside `.git`, and skip anything
that contains a **committed** file (they say so: `Keeping '...' (it contains tracked files)`).

## How to run

```bash
# macOS / Linux
./scripts/clean/clean_mac.sh --dry-run    # only list what would be deleted
./scripts/clean/clean_mac.sh              # delete
```

```powershell
# Windows
./scripts/clean/clean_win.ps1 -DryRun
./scripts/clean/clean_win.ps1
```

Run it with `--dry-run` / `-DryRun` first when you want to see the list. After cleaning, the next `dotnet build` restores and
rebuilds everything, which takes longer than usual once.

## What is deleted

Matched by name anywhere under the repository:

| Kind | Names | Why they exist |
|---|---|---|
| Build output | `bin/`, `obj/` | Compiled output and restore state of every project (framework, tests) |
| Test results | `TestResults/` | `.trx` and coverage reports written by `run-tests.sh` and the Sonar scripts |
| Test report files | `*.trx`, `coverage.cobertura.xml`, `coverage.opencover.xml` | The same reports, when a test run wrote them somewhere else |
| SonarScanner | `.sonarqube/` | Temporary working folder of `dotnet sonarscanner` |
| IDE state | `.vs/`, `*.user`, `*.suo` | Visual Studio per-user caches and settings |
| OS leftovers | `.DS_Store`, `Thumbs.db` | Finder / Explorer metadata |

The `test/` folder itself (the test **projects** and their source) is not touched, only what running them produced.

To clean more or less, edit the `DIRECTORIES` / `FILES` lists at the top of both scripts (keep the two in sync).

## Output

Like the Sonar scripts, each step prints a blue `[INFO]` banner: searching (how many items, how many kept), then the deletion
(one `deleted ./path` line per item), then `FINISHED`.

## Not covered here

- The local NuGet feed written by `scripts/pack-local.sh` (`~/nuget-local-feed`) lives outside the repository; delete it by hand if
  you want it gone.
- Docker containers, volumes and test data in your infrastructure.
