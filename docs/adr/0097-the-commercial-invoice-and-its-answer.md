# ADR-0097: The commercial invoice (TICARIFATURA) and the answer of its receiver

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

Until now an invoice was only the basic scenario (`TEMELFATURA`): it has to reach the receiver, and nothing comes back. In the commercial scenario (`TICARIFATURA`) the receiver also answers the invoice: it accepts it (`KABUL`) or refuses it (`RED`) with a UBL-TR **application response** document, which travels in an envelope of its own
(`POSTBOXENVELOPE`, from the post box of the receiver to the sender unit of the supplier), signed, through the center ([ADR-0087](0087-the-edx-mock-gib-center.md), [ADR-0093](0093-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md)). The model had the place for it ([ADR-0094](0094-the-edx-sales-invoice-and-its-envelope-are-two-statuses.md): `Responses` inside the invoice) but not the flow.

## Decision

- **The profile is chosen when the invoice is sent**: `invoiceProfile` (`TEMELFATURA`, the default, or `TICARIFATURA`) in the model, in a column of the CSV and the sheet, as an eleventh part of the `H` line of a TXT, in the gRPC message and in the create page; an uploaded UBL-TR XML says its own. It is kept on the sales invoice and on the purchase invoice (`Profile`).
- **The receiver answers from the back office** (*Alış faturaları*: *Kabul* / *Red*, a reason is required to refuse): `POST /purchase-invoices/{id}/respond` (`endpoint.purchase-invoice.respond`; the manager and the admin have it by default). Only a commercial invoice that was not answered is answered (once).
- **The answer is a document like an invoice** (`UblInvoiceAnswer` in `Edx.Shared.Efatura`: an `ApplicationResponse` from the receiver to the supplier, the invoice named by its number and uuid, `KABUL` or `RED`). The flow, all by events: the invoice service makes the unsigned document (`{tenant}/purchase/purchase_answer_draft_{ettn}.xml`) and publishes `PurchaseAnswerRequestedEto` →
  **the signer** signs it and writes `{tenant}/purchase/{year}/{month}/purchase_answer_{ettn}.xml` ([ADR-0096](0096-the-edx-invoice-container-layout.md)), `PurchaseAnswerSignedEto` → the invoice service keeps the answer in the purchase invoice's `Responses`, and **the GİB invoice integration** puts it in an envelope (`OutboundKind.InvoiceAnswer`, kind `purchase_answer`) to the sender unit
  the invoice's envelope came from, sends it like an invoice envelope and asks the center for its status; `PurchaseAnswerEnvelopeEto` (sent, delivered, refused, failed) is kept on the answer.
- **The supplier's sender unit takes the answer in** (the post box service handles `POSTBOXENVELOPE`: it is checked like an invoice, schema, signature, from and for) and publishes `SalesInvoiceAnswerReceivedEto`; the invoice service takes the answer out of the zip, keeps it (`{tenant}/sales/{year}/{month}/sales_answer_{ettn}.xml`) and puts it in the sales invoice's `Responses`. The sales invoices page shows the answer.

## Consequences

- A refused commercial invoice stays a signed, delivered invoice with a `Rejected` answer; nothing else (cancelling, returning, crediting it) is made.
- The parties of an answer have no real address (the schema wants one): the receiver's is the tenant's, the supplier's is `-`.
- The answer is made by a person; answering by rule (every invoice up to an amount, say) is not made.
- Other answers (a return, a despatch's receipt advice) and the system envelopes stay as they were ([ADR-0093](0093-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md)).

## Related

[ADR-0088](0088-the-edx-invoice-is-a-ubl-tr-document.md), [ADR-0089](0089-the-edx-integration-service-sender-unit-and-post-box.md), [ADR-0092](0092-the-edx-signer-is-a-service-of-its-own.md), [ADR-0094](0094-the-edx-sales-invoice-and-its-envelope-are-two-statuses.md), [ADR-0096](0096-the-edx-invoice-container-layout.md).
