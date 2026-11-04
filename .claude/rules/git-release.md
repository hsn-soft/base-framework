---
paths:
  - "docs/Releasing.md"
  - "docs/BranchRules.md"
  - ".github/workflows/**"
  - "scripts/versioning/**"
---

# Git workflow and releases

- Follow the branch rule in the root `CLAUDE.md` (ADR-0132): Claude works on its own `feat/...` branch cut from `dev`, commits and pushes there, and never opens a pull request into `dev` or a release branch nor merges or pushes into `dev` or `rel/*`; the maintainer does. To take what is new in `dev`, merge or rebase `dev` into the own branch.
- For the contribution flow, use [CONTRIBUTING.md](../../CONTRIBUTING.md): Conventional Commit prefixes, the repository's hooks, and the detector before the pull request the maintainer opens. Do not bypass hooks or required CI checks.
- Husky.Net is the local fast-feedback gate, not a CI replacement: pre-commit formats staged C#/JSON and rejects remaining style violations; pre-push runs a Release warnings-as-errors build and unit tests. Integration tests are intentionally left to the detector/CI because they require Docker and take longer. Install the pinned hooks with `dotnet tool restore` and `dotnet husky install`.
- If a hook fails, fix the cause and rerun it. `HUSKY=0` is an emergency bypass only; it does not count as validation because CI still enforces the checks.
- Before opening a PR, run `./scripts/ci-detector/ci_detector_mac.sh` (Windows: `./scripts/ci-detector/ci_detector_win.ps1`). `--skip-integration` / `-SkipIntegration` is an incomplete interim check, not the final PR check.
- Keep release flow as `dev` → `rel/nuget-candidate` → `rel/nuget-regular`; stable releases come from the candidate branch. Set the release `BaseVersion` as prescribed, then use the repository's scripts to derive candidate/patch versions from NuGet; do not hand-edit each package's version.
- Release branches run CI on pushes. Preserve the release/deploy workflow and its trusted NuGet publishing through OIDC; never add a long-lived NuGet API key.
- The merge table (which source may go into which target, with the special rules of the publishing branches) is [docs/BranchRules.md](../../docs/BranchRules.md); change it together with `ci-branch-rule.yml` and the ADR.
- Update [docs/Releasing.md](../../docs/Releasing.md) and the relevant ADR when release behavior changes.

Relevant decisions: ADR-0013–0014, ADR-0113–0116, ADR-0130, ADR-0132.
