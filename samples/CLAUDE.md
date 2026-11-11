# Working in `samples/`

The samples are written as if the rest of the repository did not know them ([ADR-0001](docs/adr/0001-the-samples-are-self-contained.md)). **Read [docs/adr/README.md](docs/adr/README.md) before you change a sample.**

- Everything that concerns a sample is inside `samples/`: code, comments, guides (`docs/`), decisions (`docs/adr/`), scripts (`scripts/`), ignore rules. Nothing outside `samples/` may name a sample.
- **Every decision about a sample gets an ADR here**, with the samples' own numbering; a framework decision is written *framework ADR-NNNN* and links to `../../../docs/adr`.
- Each sample has its own solution, `abc/Abc.sln`, `edx/Edx.sln` and `hsx/Hsx.sln` (not `HsnSoft.Base.sln`); add a new project to the solution of its sample, in the matching solution folder.
- **Run `./check.sh`** (restore, format check, Release build with warnings as errors, for the three samples; `./check.sh hsx` for one) before you call a change done.
- **A host project sets `<RootNamespace>` to its name without `.Http.Host`, `.Wcf.Host`, `.Grpc.Host` or `.Host`** (`Abc.OrderService`, not `Abc.OrderService.Http.Host`; [ADR-0072](docs/adr/0072-the-root-namespace-of-a-sample-host-is-the-name-of-its-project-without-the-host-suffix.md)); `./check.sh` verifies it.
- A sample references `src/` and the framework documents; the framework never references a sample.
- Never write tokens, passwords, private keys or connection strings with real values into a file; the RSA keys are git-ignored and made with `scripts/generate-rsa-keys.cs`.
- Documents and comments are in English; the back office texts are Turkish.
- **Hsx** (`hsx/`) is the long-running work: the whole of the hhs system rebuilt on this framework, not ported ([ADR-0058](docs/adr/0058-hsx-imitates-hhs-to-test-the-new-framework.md) to 0064; **the tenant, subscription and scope model, and what to decide for every new entity: `hsx/docs/TenancyModel.md` (read it before adding an entity to Hsx)**; the study of hhs and the gaps of the framework it found: `hsx/docs/hhs-analysis.md`; how to run it and its four environments: `hsx/README.md`). Abc and Edx are the references for patterns Hsx ports (events, permissions, the tenant tree).
