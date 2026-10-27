# ADR-0079: gRPC and WCF are entry points of a host, not framework packages

- **Status:** Accepted
- **Date:** 2026-10-27

## Context

The Edx intake service has to be reachable by REST, gRPC and WCF (SOAP), because its customers' systems speak one or the other. The framework had no package for gRPC or WCF. The question was whether it should have: a `HsnSoft.Base.Grpc`, a `HsnSoft.Base.Wcf`.

## Decision

**No new package. Both are entry points of the service's `Http.Host`, beside the controllers**, and call the same application service ([ADR-0041](0041-application-service-base-in-the-framework.md), [ADR-0078](0078-the-edx-sample-a-file-intake-system.md)).

- **What is the framework's already works for them:** the JWT validation (`AddBaseJwtAuthentication`), the current user and the tenant (`HttpContext` carries both for a gRPC call and for a CoreWCF call), the request and response logs (the middleware already knows `application/grpc`, its trailers and its status), the health checks. A
  package would only wrap `AddGrpc` and `AddServiceModelServices`, and what is left (the exception to status mapping, the shape of the messages) is the product's own contract.
- **gRPC:** `Grpc.AspNetCore`, a `.proto` file in the Host, `[Authorize]` with the JWT scheme, and an interceptor (`GrpcExceptionInterceptor`) that says in gRPC what the exception handler says in HTTP (`BusinessException` is `InvalidArgument` with the code in the trailer `error-code`, `NotFoundException` is `NotFound`, anything else `Internal` with a trace
  id and no cause). An archive is a **client stream** (the first message names the file, the others carry bytes), read as one `Stream` by the application service.
- **WCF:** CoreWCF (`CoreWCF.Http`, `CoreWCF.Primitives`), `BasicHttpBinding` at `/wcf/intake.svc` with its WSDL, the data contracts next to the service. CoreWCF takes no part in the authentication of the host, so **each operation asks the JWT scheme who the caller is** (`AuthenticateAsync`) and takes the application service from the request's own scope, so the
  user and the tenant are the token's. A failure is a `FaultException<IntakeFault>` with a code, the same codes as REST.
- **Validation is done by the entry point** where `[ApiController]` does not do it for it: gRPC and WCF build the model themselves, so `ModelValidation.Ensure` reads the same attributes before the application service is called.
- **Kestrel listens on two ports** (`Kestrel:Endpoints`): HTTP/1.1 for REST and WCF, HTTP/2 for gRPC (a clear-text port cannot negotiate HTTP/2, a TLS one does on one port). The **gateway passes both through**: a route with `Http2: true` (the cluster speaks HTTP/2 only) for the gRPC path, a plain route for the SOAP path; the gateway listens on two ports in the same way.
- **A framework bug found by this:** with the request logging on, a gRPC answer went out **without its message**. The logging middleware puts a tee in front of the response body, gRPC writes its message into the tee's `PipeWriter` and ends the call without flushing it, and the original body feature was put back with the message still in the buffer.
  The tee now flushes its writer before it is taken away (`ResponseBodyTee.FlushWriterAsync`), with a test.

## Consequences

- A product that needs only one of the protocols references only that package; the framework carries neither.
- The service has the `Grpc.AspNetCore` and `CoreWCF.*` references in its Host project only (`samples/Directory.Packages.props` has their versions); `Kestrel.AllowSynchronousIO` is on for WCF, which reads its messages synchronously, and so for the whole host.
- A second product that wants the exception interceptor or the token check for WCF unchanged is the moment to move them into a package (ADR-0005).

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0022](0022-web-api-answers.md), [ADR-0026](0026-route-pattern-and-jwt-by-default.md), [ADR-0027](0027-gateways-per-audience.md), [ADR-0078](0078-the-edx-sample-a-file-intake-system.md).
