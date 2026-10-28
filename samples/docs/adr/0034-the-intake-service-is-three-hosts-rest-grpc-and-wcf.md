# ADR-0034: The intake service is three hosts: REST, gRPC and WCF

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

[ADR-0027](0027-grpc-and-wcf-are-entry-points-of-a-host.md) put gRPC and WCF in the `Http.Host` of the intake service, beside the controllers. That made one process listen on two ports, made `AllowSynchronousIO` (which WCF needs) true for the REST endpoints too, and tied the three ways in to one deployment and one scaling. They are three ways for a customer to
do the same thing, and only the REST host has a reason to carry the rest of the flow.

## Decision

**The intake service is three hosts over the same Application, Domain and MongoDB projects:** `Edx.Integration.FirmInvoice.Http.Host` (REST, the back office's routes, the queue endpoints, **and the handlers of the events that carry the flow on**: the queue follows what the invoice service says), `Edx.Integration.FirmInvoice.Grpc.Host` (`intake.proto`, HTTP/2 only) and `Edx.Integration.FirmInvoice.Wcf.Host` (CoreWCF at
`/wcf/intake.svc`, `AllowSynchronousIO` on). **All three do the same upload** through `IntakeAppService`: the zip checked in memory, one correct file, a queue item in the database with **`ReceivedBy`** (the host's `Intake:ServiceName`: `Edx.Integration.FirmInvoice.Http`, `.Grpc`, `.Wcf`) and the event published. The gRPC and WCF hosts **consume no event**; their only purpose is another way in
for a client. The back office calls the REST host: it is the only one with pages' routes and the one a browser can call.

Each host has its own settings (port, log file, Kafka client name), the gateway has a cluster for each (`intake-cluster`, `intake-grpc-cluster`, `intake-wcf-cluster`) and the `back-office/` routes go to the REST host. Customer examples for the three ways in are in `samples/edx/client`.

## Consequences

- A change of the application service is deployed to three hosts; the three have the same persistence, so they share the database and the blob store.
- WCF's synchronous IO no longer touches REST, and gRPC's HTTP/2-only port is a host of its own.
- The part of ADR-0027 that placed gRPC and WCF **in the `Http.Host`** is superseded; what it says of the framework having no package for either, and of the exception mapping, the token check and the validation by the entry point, still holds.

## Related

[ADR-0027](0027-grpc-and-wcf-are-entry-points-of-a-host.md), [ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md); `samples/edx/integration/firm-invoice`, `samples/edx/client`.
