---
paths:
  - "scripts/**"
  - "samples/**"
  - ".husky/**"
  - ".config/dotnet-tools.json"
---

# Repository tooling and samples

## What lives under `scripts/`

Use the existing script for its intended job; do not create a duplicate tool.

| Path | Purpose and when to use |
|---|---|
| `scripts/run-tests.sh` | Runs all unit or integration test projects by project-name convention; shared by local work, hooks, detector, CI and Sonar. |
| `scripts/ci-detector/` | Local counterpart of GitHub CI: repository rules, formatting, Release build, tests and coverage. Run before opening a PR; its README documents stages and options. |
| `scripts/cleaner/` | Removes generated build/test/IDE output only. The detector uses it for a clean run; use `--dry-run` / `-DryRun` first for manual cleanup. Never replace it with broad deletion commands. |
| `scripts/versioning/` | Calculates release versions from the configured base version and NuGet state; its tests run in repository rules. Do not hand-calculate versions per package. |
| `scripts/pack-local.sh` | Packs Debug NuGet packages with symbols into a local feed so a consumer service can test/debug the framework package. |
| `scripts/sonar/` | Optional local SonarQube analysis, not a CI gate. It runs build and all tests then uploads reports; credentials come from the environment or ignored `scripts/sonar/.env`. |
| `scripts/graylog-fields/generate.py` | Regenerates the measured Graylog field reference after exercising a real system and querying Graylog. Credentials are environment-only; see the script and `docs/GraylogFields.md`. |

CI steps and their local equivalent are intentionally kept together in `scripts/ci-detector/`; keep detector and `.github/workflows/` behavior aligned.

## Why `samples/` exists

Samples are self-contained product systems that demonstrate realistic package composition and verify that products can select different providers. One sample can combine Kafka, MongoDB, Redis and MinIO; another can use RabbitMQ and PostgreSQL. These are consumer choices, not a reason to combine providers or add dependencies to a framework package.

- Keep each sample's source, scripts, docs, ADRs and solution inside `samples/`. The framework solution and framework docs must not depend on sample projects.
- Samples reference framework source/packages; framework packages never reference samples.
- Before changing a sample, read `samples/CLAUDE.md` and `samples/README.md`; use its own solution and run `samples/check.sh` (or its documented focused form).

## Husky.Net hooks

Husky.Net is installed from the pinned local tool manifest. `.husky/pre-commit` runs staged-file formatting and final style verification; `.husky/pre-push` runs a Release build with warnings as errors and all unit tests. Hooks catch common failures early and never commit or push on their own. Integration tests are intentionally left to the detector/CI because they require Docker and take longer.

See ADR-0006, ADR-0013–0014, `CONTRIBUTING.md`, and `scripts/ci-detector/README.md` for canonical operational details.
