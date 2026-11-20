---
paths:
  - "src/**/*.cs"
  - "src/**/*.csproj"
  - "test/**/*.cs"
  - "test/**/*.csproj"
  - "common*.props"
---

# .NET implementation rules

- Target the repository's .NET 11 conventions and import the shared build properties used by neighboring projects. `.editorconfig` is the formatting and analyzer authority; do not introduce competing style configuration.
- **Root namespaces (ADR-0158):** a framework library has `<RootNamespace />` (the folders are the namespace); a product library has its project name; a **host** project (`...Http.Host`, `...Wcf.Host`, `...Grpc.Host`, `...Host`) has `<RootNamespace>` set to its name without the host suffix, and no namespace of a host contains `Http.Host`, `Wcf.Host`, `Grpc.Host` or ends with `.Host` (a test project keeps its own name).
- Keep nullable analysis and analyzers enabled, fix warnings rather than suppressing them, and preserve the repository's warnings-as-errors build.
- In library code use explicit `using` directives, explicit types except where the type is obvious from construction, `ConfigureAwait(false)` on awaits, and `ArgumentNullException.ThrowIfNull` at public entry points.
- Emit logs through `ILogger` and `[LoggerMessage]` source-generated methods. Do not add a competing logging abstraction.
- Public service-registration extension methods need XML documentation that states what is registered and which configuration section is read.
- Never write a package version in a project file: versions are central in `Directory.Packages.props` (a `PackageReference` has no `Version`), and CA2007 (`ConfigureAwait(false)`) is an error in `src/`. The groups and the order of project references are in [architecture.md](architecture.md).
- Preserve the pre-commit style formatter's build-before-format order so compile-dependent `using` directives are not removed.
- Before changing a project file, apply the living rules in ADR-0075.

Relevant decisions: ADR-0007–0008, ADR-0043, ADR-0075–0076, ADR-0105, ADR-0123, ADR-0134.
