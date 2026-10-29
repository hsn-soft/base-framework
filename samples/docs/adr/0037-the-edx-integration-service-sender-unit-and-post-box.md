# ADR-0037: The Edx integration service is the sender unit and the post box of the customers

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

A signed invoice has to reach the center, and an invoice the center delivers for a customer has to reach that customer. In the real system an integrator does both with one **sender unit** (*gönderici birim*, alias `gb`) per customer, which wraps invoices into envelopes and hands them to the center, and one **post box** (*posta kutusu*, alias `pk`) per customer, a WCF service the center calls to deliver envelopes
([ADR-0035](0035-the-edx-mock-gib-center.md)). The invoice service must not know the protocol; it only needs to say "this invoice is signed" and hear "it was delivered" or "it was not".

## Decision

**A service of its own, `samples/edx/integration/gib-invoice`**, with two hosts: `Edx.Integration.GibInvoice.Http.Host` (the **sender unit**: the workers, the event handlers and the REST pages of the envelopes) and `Edx.Integration.GibInvoice.Wcf.Host` (the **post box**: `EFaturaPortType` of `Edx.Shared.Efatura`, which the center calls). Like the intake hosts ([ADR-0034](0034-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md)) they share the application layer and the MongoDB database;
only the Http host runs the workers and handles events.

- **Events are the only link with the invoice service.** `InvoiceSignedEto` (invoice service → integration) starts the work; `InvoiceSentEto(RecordId, InvoiceId, EnvelopeId)`, `InvoiceDeliveredEto(…, StatusCode)` and `InvoiceDeliveryFailedEto(RecordId, InvoiceId, EnvelopeId?, Stage, StatusCode, Reason)` (integration → invoice, which moves the invoice and the queue item) and
  `PurchaseInvoiceReceivedEto` (post box → invoice service, which keeps the purchase invoice) are the others (`Edx.Shared.Contracts/Events/Integration`). All run with the tenant of the invoice.
- **Outbound** (`OutboundEnvelope`, collection of its own): the sender unit takes the signed XML from the blob store, wraps it into an envelope (`SENDERENVELOPE`, from the tenant's `gb` alias to the receiver's `pk` alias, looked up in the **user list** of the center by VKN; an unknown receiver fails the delivery at once), sends it with `sendDocument` and follows it:
  `ToSend` → `Sent` (the center took it: 1000) → `Delivered` (the system response says 1300) or `Refused` (a refusal code, not tried again) or `Failed` (the center was not reachable after the retries, or never answered). A not reachable center is tried again after `SendRetryWaitSeconds`; an envelope the center has but never answered is asked about (`getApplicationResponse`) after `StatusPollAfterSeconds`.
  The outcome is the system response (an `ApplicationResponse` the center forwards to the sender unit's address, or the answer of the poll), whose code and reason are kept on the record.
- **Inbound** (`InboundEnvelope`): the post box checks the name, the MD5 and the zip, stores the zip, answers `sendDocument` at once and queues the envelope: `Queued` → `Accepted` (an invoice for a tenant that has this post box and follows the schema: the purchase invoice event was published and the post box answered the center with 1200) or `Refused` (a check failed; the post box answers with the code). A system envelope (the answer about an outbound envelope) is `Applied` to that envelope, or `Ignored` when it is not about one of ours.
  Application responses (`POSTBOXENVELOPE`: the receiver accepting or refusing an invoice) are refused by this post box.
- **The user list** (`ExchangeUser`): the registered taxpayers and their aliases, read from the center's `UserListController` at start and refreshed every `UserListRefreshMinutes`.
- **Pages:** *Entegrasyon → Zarflar* (`page.integration-envelope.view`, `endpoint.integration.envelopes.read`), outbound and inbound lists with state filters; and *Gelen faturalar* in the invoice service (`page.purchase-invoice.view`, `endpoint.invoice.purchase-invoices.read`, `…purchase-invoices.xml`). The gateway routes `integration-service`.

## Consequences

- The delivery is **at least once** (a retried `sendDocument` may deliver twice; the center's duplicate check, 1163, refuses the second), and each state change is idempotent per record.
- Not done in the sample: user envelopes (account registration), signature verification, application responses, resending a failed invoice, currencies other than TRY, and showing every invoice of an envelope with several (only the first is stored on the inbound record).
- The sample's `Participants` of the mock center and `EdxEInvoiceProfiles` must say the same thing (VKN, aliases).

## Related

[ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0034](0034-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md), [ADR-0035](0035-the-edx-mock-gib-center.md), [ADR-0036](0036-the-edx-invoice-is-a-ubl-tr-document.md); `samples/edx/integration/gib-invoice`, `samples/edx/shared/Edx.Shared.Efatura`.
