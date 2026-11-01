---
paths:
  - "docs/Releasing.md"
  - ".github/workflows/**"
  - "scripts/versioning/**"
---

# Git workflow and releases

- Follow the active rewrite constraint in the root `CLAUDE.md`: work remains on `feat/new-framework` and must not be merged into `dev` until the framework is complete. The repository instructions also prohibit stacked branches; follow them rather than inferring a different workflow from the ADR.
- For the normal contribution flow, use [CONTRIBUTING.md](../../CONTRIBUTING.md): Conventional Commit prefixes, the repository's hooks, detector before opening a PR, and PRs to `dev`. Do not bypass hooks or required CI checks.
- Husky.Net is the local fast-feedback gate, not a CI replacement: pre-commit formats staged C#/JSON and rejects remaining style violations; pre-push runs a Release warnings-as-errors build and unit tests. Integration tests are intentionally left to the detector/CI because they require Docker and take longer. Install the pinned hooks with `dotnet tool restore` and `dotnet husky install`.
- If a hook fails, fix the cause and rerun it. `HUSKY=0` is an emergency bypass only; it does not count as validation because CI still enforces the checks.
- Before opening a PR, run `./scripts/ci-detector/ci_detector_mac.sh` (Windows: `./scripts/ci-detector/ci_detector_win.ps1`). `--skip-integration` / `-SkipIntegration` is an incomplete interim check, not the final PR check.
- Keep release flow as `dev` → `rel/nuget-candidate` → `rel/nuget-regular`; stable releases come from the candidate branch. Set the release `BaseVersion` as prescribed, then use the repository's scripts to derive candidate/patch versions from NuGet; do not hand-edit each package's version.
- Release branches run CI on pushes. Preserve the release/deploy workflow and its trusted NuGet publishing through OIDC; never add a long-lived NuGet API key.
- Update [docs/Releasing.md](../../docs/Releasing.md) and the relevant ADR when release behavior changes.

Relevant decisions: ADR-0025, ADR-0013–0014, ADR-0113–0116, ADR-0130.
