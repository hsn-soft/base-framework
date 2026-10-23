# Local SonarQube analysis

Two scripts that do the same thing, one per platform. They run a full SonarQube analysis of this repository against a SonarQube
on your own machine: build, **all** tests (unit and integration) with coverage, and the upload of the results.

| File | For |
|---|---|
| `analysis_mac.sh` | Linux, macOS (bash) |
| `analysis_win.ps1` | Windows (PowerShell 7; also works with `pwsh` on macOS/Linux) |

The CI does not use them (CI gates coverage with `scripts/check-coverage.py`). They are for looking at the code quality dashboard
while you work.

## What you need

- **A running SonarQube** at `http://localhost:8031`. Start it with the compose file of this repository (it needs the
  `infra-postgres` container and the `local.infrastructure-network` network of your local infrastructure):

  ```bash
  docker compose -f docker/sonarqube.compose.yml up -d
  ```

  The first start takes about a minute. Log in at `http://localhost:8031` (`admin` / `admin` on a fresh install; SonarQube asks you to
  change the password).
- **A token.** In SonarQube: *My Account > Security > Generate Tokens* (type *User token*). Treat it like a password and never
  commit it. Keep it in `scripts/sonar/.env` (next to the scripts; copy `.env.example` and fill in `SONAR_TOKEN=`). Git ignores
  `*.env` files, so it is never uploaded. A variable already set in your shell wins over the file.
- **Docker running**, because the integration tests start their services (RabbitMQ, Kafka, Redis, MongoDB, PostgreSQL, SQL Server,
  MinIO) with Testcontainers. SQL Server needs about 2 GB of memory in Docker.
- The .NET SDK. The scripts install the `dotnet-sonarscanner` global tool on first use.

## How to run

From anywhere (the scripts find the repository root and the `.env` themselves):

```bash
# Linux / macOS
./scripts/sonar/analysis_mac.sh
```

```powershell
# Windows
./scripts/sonar/analysis_win.ps1
```

Without a `.env` file, pass the token through the environment instead: `SONAR_TOKEN=<token> ./scripts/sonar/analysis_mac.sh` or
`$env:SONAR_TOKEN = "<token>"` before the PowerShell script.

| Setting | Default | Meaning |
|---|---|---|
| `SONAR_TOKEN` | required (`.env` or environment) | Token of the user that owns the analysis |
| `SONAR_HOST_URL` | `http://localhost:8031` | Where SonarQube runs |
| `SONAR_PROJECT_KEY` | `hsnsoft-base-framework` | The project key; the project is created on the first analysis |

A full run takes about 3-4 minutes. When it ends it prints the dashboard link, e.g.
`http://localhost:8031/dashboard?id=hsnsoft-base-framework`. SonarQube processes the uploaded report in the background, so the
numbers may appear a few seconds after the script finishes.

## What the scripts do

Each step prints a blue `[INFO]` banner, so you can see where a run is:

1. **Check the environment**: SonarQube answers and is `UP`, Docker is running. Stops with a message otherwise.
2. **Install the scanner**: `dotnet-sonarscanner` global tool, only if missing.
3. **Clean `TestResults/`**: the folder the reports are written to (git ignores it).
4. **`sonarscanner begin`**: tells the scanner where the test and coverage reports will be.
5. **Restore and build** in `Release`.
6. **Test every project under `test/`** with `test/coverlet.runsettings`, writing `.trx` results and OpenCover coverage to
   `TestResults/<project>/`. A failing test stops the run, so you never upload half a picture.
7. **`sonarscanner end`**: uploads the sources, the issues the Roslyn analyzers found, and the results.

## What is analysed

Only the packages under `src/` count. These are left out of the analysis and of the coverage figure:

- `test/**`, `samples/**` and `scripts/**` (tests, sample applications and tooling are not the product)
- `**/Migrations/**`, `**/tmp/**`, files named `*Test*`
- only `*.cs`, `*.js` and `*.css` files are inspected

The exclusion lists are the `/d:sonar.exclusions` and `/d:sonar.coverage.exclusions` lines in both scripts; keep the two scripts in
sync when you change one.

## Troubleshooting

| Message / symptom | Cause and fix |
|---|---|
| `SonarQube is not reachable` | The container is not running or is still starting. `docker compose -f docker/sonarqube.compose.yml up -d`, wait for `UP` at `/api/system/status`. |
| `Docker is not running` | Start Docker Desktop / the daemon. |
| `SONAR_TOKEN is required` | Create `scripts/sonar/.env` from `.env.example` and set the token, or export the variable. |
| `401` while uploading | The token was revoked or typed wrongly; generate a new one. |
| An integration test fails to start a container | Not enough memory for Docker (SQL Server needs ~2 GB), or the image is still downloading. Re-run. MongoDB 8 does not start on Linux kernel 6.19+; the tests use `mongo:7.0`. |
| The project is not on the dashboard yet | The report is still being processed; refresh after a few seconds. |
| Coverage is a little lower than `scripts/check-coverage.py` reports | Sonar counts the lines of auto-property initializers in the option classes that coverlet does not report. These are not real gaps. |

## Starting from scratch

To rebuild SonarQube from nothing (this deletes all analysis history):

```bash
docker rm -f infra-sonarqube_container
docker exec infra-postgres_container psql -U postgres -c "DROP DATABASE IF EXISTS sonarqube WITH (FORCE);" -c "CREATE DATABASE sonarqube;"
rm -rf ~/docker-volumes/infra/sonarqube && mkdir -p ~/docker-volumes/infra/sonarqube/{conf,data,extensions,logs,temp}
docker compose -f docker/sonarqube.compose.yml up -d
```

Then log in with `admin` / `admin`, change the password, generate a new token and run the analysis again.
