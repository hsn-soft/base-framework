# ADR-0158: The root namespace of a host is the name of its project without the host suffix

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

A host project is named for what it is and how it is reached: `Xyz.OrderService.Http.Host`, `Xyz.Invoice.Wcf.Host`, `Xyz.Gateway.Host`. The project name is also what the SDK takes as the root namespace, so the code of a host lived in `Xyz.OrderService.Http.Host.Controllers`,
and two hosts of the same service (an HTTP one and a WCF one) shared nothing in their namespaces although they are one service. A namespace says what the code belongs to, not how it is hosted.

## Decision

- **A framework library** has an empty root namespace (`<RootNamespace />`): the namespace is the folder hierarchy under the project folder. This is how the framework is written today.
- **A library of a product** (shared libraries, and the domain, application and persistence projects of a service) has the project name as its root namespace, as the SDK gives it.
- **A host project** (a project of the web SDK, or a console host, that is the executable of a service: its name ends with `.Http.Host`, `.Wcf.Host`, `.Grpc.Host` or `.Host`) has `<RootNamespace>` set to its name **without that suffix**: `Xyz.OrderService.Http.Host` is `Xyz.OrderService`, `Xyz.Integration.GibInvoice.Wcf.Host` and `Xyz.Integration.GibInvoice.Http.Host` are both `Xyz.Integration.GibInvoice`.
  The assembly name and the folder keep the full name. No namespace in the code of a host contains `Http.Host`, `Wcf.Host`, `Grpc.Host` or ends with `.Host`; a test project keeps its own name (`...Http.Host.Tests`).
- A generated namespace follows the same rule (the `csharp_namespace` of a proto file of a gRPC host is the root namespace of the host).

## Consequences

- The code of every host of a service is in one namespace tree, whatever the transport.
- The namespaces of the hosts of the samples and of every product built on the framework were renamed (the `Host` segment is gone); the usings of the tests were renamed with them.
- A host project needs an explicit `<RootNamespace>`; a new host without it gets the long name from the SDK, and the convention test of the repository (or `samples/check.sh`) says so.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0006](0006-repository-folders-and-the-solution-file.md); `.claude/rules/dotnet.md`; the samples' own ADR on the same rule.
