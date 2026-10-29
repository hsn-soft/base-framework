# ADR-0043: "Intake" is "FirmInvoice" everywhere: routes, contracts, permissions, settings and code

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

[ADR-0038](0038-edx-services-are-named-integration-firm-invoice-and-integration-gib-invoice.md) renamed the projects of the intake service to `Edx.Integration.FirmInvoice.*` but left the word *intake* in the public routes, the gRPC and WCF contracts, the permission codes, the settings and the code (`IntakeAppService`, `intake-service`, `endpoint.intake.upload`, `edx.intake.v1`). The same thing was called two names, which confused readers.
The mock center's `EnvelopeIntake` (the center taking in an envelope) is another thing and keeps its name.

## Decision

The word *intake* is replaced by *firm invoice* in every name of the firm invoice integration:

- **Routes:** `/api/firm-invoice-service/v1/commercial/...` and `/api/firm-invoice-service/v1/back-office/...` (the back office, the customer examples, the Python client and the smoke test call them), WCF `/wcf/firm-invoice.svc`.
- **gRPC:** the proto is `firm_invoice.proto`, package `edx.firm_invoice.v1`, service `FirmInvoiceService`. **WCF:** the contract is `IFirmInvoiceService` (`urn:edx:firm-invoice:...`).
- **Permission codes:** `endpoint.firm-invoice.upload`, `.submit-model`, `.files.get`, `endpoint.firm-invoice-back-office.*`, `operation.firm-invoice.*`, `constraint.firm-invoice.upload.max-bytes`.
- **Settings:** the section `FirmInvoice` (`FirmInvoice:ServiceName`, `FirmInvoice:MaxUploadBytes`) and the connection string `Mongo-FirmInvoiceService`.
- **Code:** `FirmInvoiceAppService`, `FirmInvoiceOptions`, `MongoFirmInvoiceDbProperties`, `AddFirmInvoicePersistence`, the folders `FirmInvoiceDomain` and `Events/FirmInvoice`.

This supersedes the "what is not renamed" part of ADR-0038 (and the route names of [ADR-0039](0039-the-edx-integrations-are-reached-directly-the-services-through-the-gateway.md)). Older ADRs keep the words they were written with.

## Consequences

- The permission codes changed, and the grants of the roles are in the identity database: a development identity database is dropped and made again at the next start (the seeder gives the default grants).
- A customer's program that used the old routes, the old gRPC package or the old WCF address has to change them (the sample's examples did).

## Related

[ADR-0034](0034-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md), [ADR-0038](0038-edx-services-are-named-integration-firm-invoice-and-integration-gib-invoice.md), [ADR-0039](0039-the-edx-integrations-are-reached-directly-the-services-through-the-gateway.md).
