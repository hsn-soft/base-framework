# ADR-0042: A sales invoice and its envelope have two statuses; the envelope belongs to the GİB invoice integration

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The invoice entity of the invoice service had one status that carried two lives: the invoice's (processing, draft, approved, signed) and its envelope's (sent, delivered, delivery failed). A back-office user lists the sales invoices and wants both the invoice status and the envelope status, and for the invoices that came in the status of their envelopes at the center.
The first model of the sender side (the `eds` services) kept five near-identical envelope entities and about twelve status bytes on the invoice row. The center keeps the status on the **envelope** (one of the codes of Ek-2), never on its invoices; an envelope holds up to 1000 invoices of one post box ([ADR-0041](0041-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md)).

## Decision

- **`SalesInvoice` and `PurchaseInvoice`** (the invoice service; `Invoice` is renamed `SalesInvoice`, its routes are `/sales-invoices`, its permission codes stay) are the two kinds of invoice. A sales invoice's own status ends when it is signed: `Processing`, `Failed`, `DraftCreated`, `Rejected`, `Approved`, `Signed`. `Sent`, `Delivered` and `DeliveryFailed` are no longer
  invoice statuses.
- **The envelope is the GİB invoice integration's.** It keeps the envelope (`OutboundEnvelope`, `InboundEnvelope`) in its own database with an embedded `History` of `EnvelopeEvent`s (time, code of Ek-2, text, who said it). The invoice service keeps only a **summary** on the invoice, filled by the integration's events (`InvoiceSentEto`, `InvoiceDeliveredEto`,
  `InvoiceDeliveryFailedEto`, and for incoming envelopes `PurchaseEnvelopeStatusEto`): a sales invoice has `EnvelopeId`, `EnvelopeState` (`None`, `ToSend`, `Sent`, `Delivered`, `Refused`, `Failed`; it only goes forward, so events that arrive out of order are harmless), the last code and text of the center and the failure note; a purchase invoice has `EnvelopeId` and the last
  code and text the center said of the envelope it came in (1200, 1220, 1300...). The list shows both statuses from one query. **The detail of an envelope is asked of the integration with the uuid of the envelope** (`POST integration-service/v1/envelopes/{outbound|inbound}/by-envelope/{uuid}`): the back office opens it from the list.
- **The history and the answers are embedded in the invoice** (Mongo): `History` (`Status` and `Envelope` entries: what, when, who, why) and `Responses` (the answers of the receiver to a commercial invoice: accepted or rejected, the number and uuid of the response document, the envelope it came in). **Only the shape of `Responses` exists**: making, sending and receiving application responses
  is not done. A purchase invoice has the same two lists.
- **The post box asks the center** about the envelopes it took (`InboundStatusPoller`: with the uuid, until the center ends them) and tells the invoice service the new status.
- The queue item of an invoice (intake) still completes when its envelope is delivered and fails (stage `Delivery`) when it does not.
- The pages are *Satış faturaları* and *Alış faturaları*; not added now: `ArchiveYear`, `ClientId`, a `Customer` directory.

## Consequences

- A signed invoice whose envelope failed stays `Signed` (its envelope says `Refused` or `Failed` with the reason); resending it is not done (it was not before either), so `IsReplaceable` is only failed and rejected records.
- The envelope's data lives once (the integration's) and the invoice carries a few fields of it; a change of the envelope's states changes the integration, the event and the summary.
- The center's status of an envelope that came in is read by a poll (and shown), so the receiver sees 1220 turn into 1300.

## Related

[ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0036](0036-the-edx-invoice-is-a-ubl-tr-document.md), [ADR-0037](0037-the-edx-integration-service-sender-unit-and-post-box.md), [ADR-0041](0041-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md); `samples/edx/service/invoice`, `samples/edx/integration/gib-invoice`.
