# ADR-0044: The layout of the storage: a work folder, then everything of a tenant together, and the same folders in both queues

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The invoice service kept every step of an invoice in a folder of its own (`uploads`, `extracted`, `draft`, `signed`, `purchase`), with the id of the tenant in each and the queue id in front of the file names; the zip stayed after it was unpacked and the extracted file stayed after the draft was made. A person looking in the container could not tell what is work in progress and what is the invoice.

## Decision

This supersedes the object paths of [ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md) and [ADR-0040](0040-the-edx-signer-is-a-service-of-its-own.md). The container of the invoice service (`edx-invoice`) holds **the work of the import in one folder and the invoices in folders by tenant**:

| Object | Who writes it, and when |
|---|---|
| `firm-invoice-tmp/{queueId}_{file}.zip` | the invoice service copies the zip of the firm invoice integration here (the queue id is unique: no folder of the tenant); **deleted** once the file in it is unpacked |
| `firm-invoice-tmp/{queueId}_{file}.csv` | the unpacked file, beside it; **deleted** once the draft is kept (and renamed `error_…` when the import fails) |
| `{tenant}/sales/draft_{ettn}.xml` | the UBL-TR draft of the sales invoice, until it is signed (then **deleted** by the invoice service) |
| `{tenant}/sales/{year}/{month}/sales_{ettn}.xml` | the signed invoice, written by the **signer**; the invoice service says where in `InvoiceApprovedEto.SignedPath`; the year and the month are those of the invoice's issue date |
| `{tenant}/purchase/{year}/{month}/purchase_{ettn}.xml` | an invoice that came in (the invoice service copies it from the GİB invoice integration's container), by the issue date |

Everything of a customer is under its `{tenant}` (its sales invoices, its purchase invoices, and the answers to its invoices when they are made: `{tenant}/…`). **The two queue containers hold nothing but zips, tenant first, in the same time folders** `{year}/{month}{day}/{hour}{minute}` (UTC):

- the firm invoice integration (`edx-integration-firm-invoice`): `{tenant}/{year}/{month}{day}/{hour}{minute}/{queueId}_invoice.zip` (whatever the customer called the zip; a model submitted as JSON or XML is zipped first), and the answer to it, when answers are made, `{queueId}_invoice_answer.zip` beside it;
- the GİB invoice integration (`edx-integration-gib-invoice`): `{tenant}/{kind}/{year}/{month}{day}/{hour}{minute}/{name}_{envelope}.zip`, the kind being `sales` (our sales invoices, to the center: `sales_…`), `sales_answer` (the receivers' answers to them: `sales_answer_…`), `purchase` (the invoices that came in: `purchase_…`),
  `purchase_answer` (our answers to them: `purchase_answer_…`) and `system` (system envelopes: `inbound_…` and `outbound_…`). An incoming envelope for a post box no tenant owns is kept under `unassigned/`. The invoices of an incoming envelope are not taken out as files: the invoice service takes each out of the zip (by its uuid) when it keeps the purchase invoice.

**Every id in a name is a uuid without dashes in lowercase** (`{tenant}`, `{queueId}`, `{ettn}`, the envelope), `{month}` has two digits. The GİB invoice integration reads the signed object it is told of (`InvoiceSignedEto.ObjectName`).

## Consequences

- After an import only the draft stays in the container; after the signing only the signed invoice does. Nothing but the failed import's files is left in the work folder.
- The paths are the invoice service's to decide (the signer only writes where it is told).

## Related

[ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0040](0040-the-edx-signer-is-a-service-of-its-own.md); `samples/edx/service/invoice`.
