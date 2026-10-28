# ADR-0085: The Edx invoice pipeline: queue, import, draft, approve, sign

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

[ADR-0078](0078-the-edx-sample-a-file-intake-system.md) made the Edx intake service keep the files it received, and the invoice service only recorded a few facts about them. The system is to do what its name says: customers send **an invoice** (a zip with one file, or a model) and it has to
become a **signed XML** after a person approved it. That needs a queue the sender can read, an import that is told what went wrong, rules for an invoice that is sent twice or sent again after a failure, and a place for the person.

## Decision

**One pipeline, four steps, two services, events in between** (each event is an `IIntegrationEventMessage` of `Edx.Shared.Contracts`, so the envelope carries the user and the tenant and every handler works as the one who sent or approved).

1. **The door (intake service, synchronous).** The sender gives the zip, the **uuid** of the invoice (`invoiceId`), its **number**, the **type** of the file in the zip (`Xml`, `Txt`, `Csv`, `Xls`; a model is `Model`) and the **SHA-256 of the zip**. The service checks in memory (the hash, that it is a zip with exactly one file, that the file's extension is its type, that an XML is well formed, the
   size limit of the caller's role), writes the zip to the blob store, writes a **queue item** (`Queued`, with the channel, the source type and `ReceivedBy`, the host that received it) and publishes `InvoiceQueuedEto`. What is wrong is answered at once (400, with a code); there is no disk, only the store.
2. **The import (invoice service, on `InvoiceQueuedEto`).** It **claims** the invoice (the store decides, see below), copies the zip to its own container under `uploads/`, unpacks the one file to `extracted/{queueId}_{name}`, reads it by its type (CSV and sheets by their columns, TXT by `H|`/`L|` lines, XML by the **XSD made from the data contract** `InvoiceSubmissionDto`, a model as JSON or XML), checks it
   (the uuid and the number in the file are the sender's, the data annotations) and the **constraint of the sender** (`constraint.invoice.max-create-amount`, as the sender), then writes `draft/draft_{invoiceId}.xml` and the record becomes `DraftCreated` (`InvoiceDraftCreatedEto`). Whatever goes wrong is the invoice's **failure** (`Failed`, with the stage `Claim`/`Fetch`/`Extract`/`Validate`/`Limit`/`Convert`
   and the reason; its stored objects are renamed with `error_` in front) and `InvoiceImportFailedEto` tells the queue. A failure is not retried: the sender corrects the file.
3. **The person (back office, draft approval page).** A manager approves (`constraint.invoice.max-approve-amount`) or rejects with a reason (`InvoiceRejectedEto`). The XML of a draft can be read by who has both operations that hide the tax ids and the prices.
4. **The signature (invoice service, on its own `InvoiceApprovedEto`).** Approving only records it and tells the service itself, so the signing runs apart from the request, as the approver. The **signer** (`IInvoiceSigner`; the sample's is a fake that adds a `Signature` element) writes `signed/signed_{invoiceId}.xml`, the draft is deleted, the record is `Signed` and
   `InvoiceCompletedEto` makes the queue item `Completed`.

**Statuses.** Queue item: `Queued`, `DraftCreated`, `Rejected`, `Failed`, `Completed`; it only moves forward, because the events are on topics of their own and do not arrive in order. Invoice record: `Processing`, `Failed`, `DraftCreated`, `Rejected`, `Approved`, `Signed`.

**Keys and duplicates.** The primary key of an invoice record (and of a queue item) is its own id, new each time an invoice is sent; `InvoiceId` is the sender's uuid and **repeats**. At most one record of an invoice is **live** (`Processing`, `DraftCreated`, `Approved`, `Signed`): two **partial unique indexes** over the live, not deleted records, by tenant and uuid and by tenant and number, make the store
decide when two arrive at once (`TryAddAsync` is false on a duplicate key; nothing changes). So: an invoice that is `Processing`, a draft, approved or signed **cannot be sent again** (the new queue item fails with the reason; a rejected draft, or another number, is the way out); one that is `Failed` or `Rejected` **may be sent again with the same uuid and number**, and when
its replacement reaches `DraftCreated` the old records and their queue items are **deleted softly** (`InvoiceSupersededEto`); until then they stay visible with their reasons. A question about an invoice answers with the record that got through, otherwise with all of them.

**The sender's and the viewer's limits** are constraints and operations as in [ADR-0083](0083-permission-types-the-menu-and-constraints-that-must-be-given.md): a list and a read hold the invoices up to `max-view-amount` (one without a total yet is shown), amounts and tax ids are operations.

## Consequences

- The sender is answered "queued" and reads the result later; there is no transaction over the store, the database and the broker (an outbox would close the gap; the system has none yet), and a record left `Processing` by a service that stopped blocks its invoice until somebody looks at it.
- The intake service knows nothing of schemas, and the invoice service nothing of zips as a wire format: each does its half.
- The tables a customer sends (columns, `H|`/`L|` lines) are the system's own formats, documented in `samples/edx/docs`; a customer's own format would be another parser in the import.
- Everything of the old pipeline (`StoredFile`, `ReceivedFile`, the create/update/delete of an invoice) is gone: an invoice is made by being sent.

## Related

[ADR-0078](0078-the-edx-sample-a-file-intake-system.md), [ADR-0081](0081-permissions-a-package-grants-in-the-cache-and-one-service-that-manages-them.md), [ADR-0083](0083-permission-types-the-menu-and-constraints-that-must-be-given.md), [ADR-0084](0084-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md),
[ADR-0086](0086-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md); `samples/edx/docs/README.md` (the formats), `samples/edx/smoke-test.sh` (the whole pipeline, run).
