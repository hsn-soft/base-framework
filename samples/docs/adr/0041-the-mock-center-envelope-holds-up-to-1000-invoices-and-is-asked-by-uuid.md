# ADR-0093: In the mock center an envelope is the post: up to 1000 invoices for one post box; its status is asked by uuid; system envelopes are off

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

[ADR-0087](0087-the-edx-mock-gib-center.md) made the mock center keep one record per envelope. Two things of the real center were not said: an envelope is **the post** (it holds many invoices, all for the same post box: the envelope has one receiver, and each invoice is for that receiver), and the sender
can learn what became of an envelope without waiting for a system envelope, by asking the center with the uuid of the envelope. The system envelope (the center's answer pushed to the sender unit, the post box's answer pushed to the center) doubles the ways a status can arrive and adds a lot of code to both ends.

## Decision

- **An envelope holds up to `Center:MaxDocumentsPerEnvelope` (1000) invoices, all for the post box it is addressed to.** The analysis refuses more (1140) and an invoice whose customer is not the receiver of the envelope (1181: an envelope holds the invoices of one post box). The record keeps `DocumentCount` and the keys of the documents (the number with the sender, and the uuid) that the duplicate check (1163) uses.
- **The status belongs to the envelope, not to its invoices**, and is one of the codes of Ek-2 (1000 queued, 1100 being analyzed, 1110 to 1195 refused, 1200 processed, 1210 and 1215 not delivered, 1220 waiting for the post box's answer, 1230 the answer failed, 1300 completed). The center keeps it on the envelope record with the stage; `getApplicationResponse` with the uuid of the envelope (and nothing else) answers with it.
- **System envelopes are off:** `Center:SendSystemResponses` (the center pushes none to the sender) and `Center:SendSystemResponses` of the GİB invoice integration (its post box pushes none to the center) default to false. The sender unit asks the center (`StatusPollAfterSeconds`), and the center asks the post box for its answer to an invoice envelope (`AnswerPollAfterSeconds`). A system envelope that does come is still applied, so
  the code stays and can be turned on.
- The invoice's own lifecycle ends when it is signed; the envelope's ends when it is placed at the receiver's system (1300) or fails (1215, 1230, a refusal). The two are kept apart ([ADR-0094](0094-the-edx-sales-invoice-and-its-envelope-are-two-statuses.md)).

## Consequences

- The mock is closer to the real center for a batch of invoices; the tools `send-batch.cs` (one envelope, N invoices, `mixed` for a wrong receiver) and `send-invoice.cs` exercise it.
- The status of a sent envelope reaches the sender only by the poll: in development the polls are a few seconds, in production values they are minutes.
- Application responses (accept or refuse of a commercial invoice, which travel in envelopes of their own) are still not made.

## Related

[ADR-0087](0087-the-edx-mock-gib-center.md), [ADR-0089](0089-the-edx-integration-service-sender-unit-and-post-box.md); `samples/edx/mock`, `samples/edx/mock/tools/send-batch.cs`.
