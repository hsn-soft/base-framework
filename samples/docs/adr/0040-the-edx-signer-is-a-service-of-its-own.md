# ADR-0092: The Edx signer is a service of its own: `Edx.SignerService.Http.Host`

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

The invoice service signed the invoice a person approved with an `IInvoiceSigner` of its own ([ADR-0085](0085-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0088](0088-the-edx-invoice-is-a-ubl-tr-document.md)); the comment of the interface already said that the real signer (a financial seal, a certificate, an HSM) is a service of its own. The key of a seal is the most guarded thing of an e-invoice system, and it is
not the invoice service's business: it must be deployable, scaled and secured apart, and replaceable by a real signer without touching the invoice.

## Decision

- **`samples/edx/service/signer/Edx.SignerService.Http.Host`** is a microservice (a service, so it is reached through the gateway, [ADR-0091](0091-the-edx-integrations-are-reached-directly-the-services-through-the-gateway.md); port `5550`, a health route `/signer-service/health`). It is **one project**: it has no database and no Redis (it has no endpoint
  that needs a permission), only Kafka, the tenant of the messages and the blob store.
- **It works on events:** the invoice service publishes `InvoiceApprovedEto` when a person approves a draft (carrying what the signer needs: the container and the path of the draft, the signer's name and what the next service needs to hear about the invoice); the signer reads the draft, signs it (`IInvoiceSigner`, today `FakeInvoiceSigner`: a signature of the right shape with made-up
  values), checks that the signed document still follows the UBL-TR schema, writes it to `signed/{tenant}/` of the same container and publishes **`InvoiceSignedEto`**. The invoice service handles it (the record moves to `Signed`, the draft is deleted), and so does the GİB invoice integration (it makes the envelope and sends it).
- The signing is idempotent in effect and a failure is thrown, so the bus tries again (three attempts); the invoice stays `Approved` until it is signed.

## Consequences

- The invoice service no longer knows how to sign: `IInvoiceSigner` and `FakeInvoiceSigner` moved to the signer, and the invoice service has one handler less (`InvoiceApprovedHandler`) and one more (`InvoiceSignedHandler`). `InvoiceApprovedEto` carries more fields.
- An invoice waits `Approved` while the signer is down; a late `InvoiceSentEto` that reaches the invoice service before its own `InvoiceSignedEto` has been applied is not applied (the record is not `Signed` yet): the two consumers of `InvoiceSignedEto` race, and the center's answer takes longer than the invoice service's write in practice.
- A real signer replaces `FakeInvoiceSigner` in this one host (and gets its key from a vault, never a file of the repository).

## Related

[ADR-0085](0085-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0088](0088-the-edx-invoice-is-a-ubl-tr-document.md), [ADR-0089](0089-the-edx-integration-service-sender-unit-and-post-box.md), [ADR-0091](0091-the-edx-integrations-are-reached-directly-the-services-through-the-gateway.md); `samples/edx/service/signer`.
