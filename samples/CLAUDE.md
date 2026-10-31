# Working in `samples/`

The samples are written as if the rest of the repository did not know them ([ADR-0001](docs/adr/0001-the-samples-are-self-contained.md)). **Read [docs/adr/README.md](docs/adr/README.md) before you change a sample.**

- Everything that concerns a sample is inside `samples/`: code, comments, guides (`docs/`), decisions (`docs/adr/`), scripts (`scripts/`), ignore rules. Nothing outside `samples/` may name a sample.
- **Every decision about a sample gets an ADR here**, with the samples' own numbering; a framework decision is written *framework ADR-NNNN* and links to `../../../docs/adr`.
- Each sample has its own solution, `abc/Abc.sln` and `edx/Edx.sln` (not `HsnSoft.Base.sln`); add a new project to the solution of its sample, in the matching solution folder.
- **Run `./check.sh`** (restore, format check, Release build with warnings as errors, for both samples; `./check.sh abc` for one) before you call a change done.
- A sample references `src/` and the framework documents; the framework never references a sample.
- Never write tokens, passwords, private keys or connection strings with real values into a file; the RSA keys are git-ignored and made with `scripts/generate-rsa-keys.cs`.
- Documents and comments are in English; the back office texts are Turkish.
