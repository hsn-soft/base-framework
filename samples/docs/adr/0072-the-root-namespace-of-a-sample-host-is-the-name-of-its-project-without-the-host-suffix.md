# ADR-0072: The root namespace of a sample host is the name of its project without the host suffix

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

The hosts of the samples (`Abc.OrderService.Http.Host`, `Hsx.AuthServer.Host`, `Edx.Integration.GibInvoice.Wcf.Host`, ...) took the project name as their root namespace, so the code of a service was in `Abc.OrderService.Http.Host.Controllers` while its domain and
application were in `Abc.OrderService.Domain` and `Abc.OrderService.Application`. A namespace says what the code belongs to, not how it is hosted; the HTTP, WCF and gRPC hosts of one service (Edx integrations) are one service.

## Decision

[framework ADR-0158](../../../docs/adr/0158-the-root-namespace-of-a-host-is-the-name-of-its-project-without-the-host-suffix.md) applies to every host of `abc`, `edx` and `hsx`: `<RootNamespace>` is the name without `.Http.Host`, `.Wcf.Host`, `.Grpc.Host` or `.Host`
(`Abc.OrderService`, `Hsx.AuthServer`, `Edx.Integration.GibInvoice` for both its hosts). The assembly names and folders do not change. The `csharp_namespace` of the proto file of the Edx gRPC host is the root namespace of the host. `./check.sh` fails when a host has no
such `<RootNamespace>` or when a namespace of a host still has the suffix.

## Consequences

- The namespaces and usings of every host and of the tests that name them were renamed in one change; the Razor pages of the web hosts too (`@model`).
- A new host of a sample sets `<RootNamespace>` in its project file.

## Related

[framework ADR-0158](../../../docs/adr/0158-the-root-namespace-of-a-host-is-the-name-of-its-project-without-the-host-suffix.md); `samples/check.sh`.
