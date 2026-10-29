# ADR-0038: The Edx integration services are named by what they integrate: `Edx.Integration.FirmInvoice.*` and `Edx.Integration.GibInvoice.*`

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The two services that move invoices across the border of the system were named `intake` (what the firms, the customers, send in) and `integration` (the sender unit and post box towards the center), both in `service/`. The names said little, and `Edx.IntakeService.Grpc.Host` read as a service with a protocol, not as one side of an integration. There are two integrations, with two parties, and more could follow (other document types).

## Decision

A service that integrates the system with a party is named **`Edx.Integration.<Party><Document>.<Layer>`** and lives in `samples/edx/integration/<party>-<document>/` (beside `service/`, which keeps the services that are not an integration: identity and invoice):

- the **firm invoice** integration (what a customer sends us: REST, gRPC, WCF; formerly the intake service): `samples/edx/integration/firm-invoice`, projects `Edx.Integration.FirmInvoice.{Domain,Application,MongoDB,Http.Host,Grpc.Host,Wcf.Host}`;
- the **GİB invoice** integration (the sender unit and the post box towards the center; formerly the integration service): `samples/edx/integration/gib-invoice`, projects `Edx.Integration.GibInvoice.{Domain,Application,MongoDB,Http.Host,Wcf.Host}`.

Namespaces follow the project names, and so do the service names the hosts report (`ReceivedBy` of a queue item is `Edx.Integration.FirmInvoice.Http`, `.Grpc` or `.Wcf`). The identity service, the invoice service and the shared projects keep their names.

**The databases and the Kafka consumer client names follow the new names** (`Edx_IntegrationFirm_*`, `Edx_IntegrationGib_*`; `IntegrationFirmInvoiceApi`, `IntegrationFirmInvoiceGrpc`, `IntegrationFirmInvoiceWcf`, `IntegrationGibInvoiceApi`, `IntegrationGibInvoiceWcf`; the identity, invoice and signer services keep `InvoiceService`-style names), and the invoice collection is `SalesInvoice`. Renaming them moves the data and the consumer groups: a development database is dropped. **What is not renamed:** the public routes of the gateway (`intake-service/...`, `integration-service/...`), the names of the connection strings, the WCF contract name and the permission codes (`endpoint.intake.*`, `endpoint.integration.*`); they are the interface of the clients or hold data, and a rename would change them for no gain. They can follow in a later ADR if wanted.

## Consequences

- The paths and project names in [ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md) to [ADR-0037](0037-the-edx-integration-service-sender-unit-and-post-box.md) were updated to the new names; their decisions are unchanged. The solution folders, `run-all.sh`, the smoke test, the references and the documents are in step.
- A later integration is a new folder and a new set of projects in the same pattern.

## Related

[ADR-0034](0034-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md), [ADR-0037](0037-the-edx-integration-service-sender-unit-and-post-box.md).
