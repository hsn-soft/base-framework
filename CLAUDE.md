# Working in this repository

This is the HsnSoft Base Framework: small, single-capability NuGet packages, their tests, and example systems. **Before you change anything, read [docs/adr/README.md](docs/adr/README.md)**: the decisions that
shape the repository (package rules, where code lives, the build, tests, CI, hooks, logging, ...) are recorded there as ADRs, with their reasons. The reference documents are in [docs/](docs/README.md) and the way we work in
[CONTRIBUTING.md](CONTRIBUTING.md).

- **All the work of the rewrite is committed on `feat/new-framework`** and is not merged into `dev` until the whole framework exists (ADR-0025); do not open a merging pull request into `dev` and do not create stacked branches.
- **Every architecture decision gets an ADR**, written when it is taken, in the same change: copy `docs/adr/0000-template.md` to the next number and add a row to the index. A decision is never edited into another
  one; a new ADR supersedes the old (see ADR-0001). Read the ADRs of an area before proposing something that touches it.
- **Run `./scripts/detector/detector_mac.sh` before you call a change done** (and `dotnet format`); it runs the CI's checks. After a change that touches the samples, run them (`samples/xyz/run-all.sh`, `smoke-test.sh`) too: the detector only builds them.
- **When you add a folder or a project, add it to `HsnSoft.Base.sln` in the matching solution folder** (`src`, `test`, `samples/<system>`); read the file first, the maintainer edits it by hand (ADR-0006).
- Documents and code comments are in English; the maintainer talks Turkish in conversation.
- Never write tokens, passwords, private keys or connection strings with real values into a file; Graylog and SonarQube credentials come from the environment or an ignored `.env`. RSA keys of the samples are made with `scripts/generate-rsa-keys.cs` and are git-ignored (ADR-0031); never reuse a key of another system.
