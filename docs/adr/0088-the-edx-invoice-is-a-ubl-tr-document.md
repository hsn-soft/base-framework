# ADR-0088: The Edx invoice is a UBL-TR 1.2.1 document, made from the customer's file and checked against the schema

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

Until now an Edx invoice was a small model of our own (a number, a supplier, lines) and its "XML" a draft that only resembled an invoice. The invoice is now to go to the center ([ADR-0087](0087-the-edx-mock-gib-center.md)), which accepts only a signed **UBL-TR 1.2.1** invoice (UBL 2.1, `CustomizationID` TR1.2, profile `TEMELFATURA`, type `SATIS`) that follows the schema in `samples/edx/docs/einvoice/UBLTR_1.2.1_Paketi/xsdrt` (main document `UBL-Invoice-2.1.xsd`).
The package has no schematron rules, so the schema is the check. A customer of the integrator does not know UBL: they have a sheet, a text file or a model, and they say who the invoice is for, not who it is from.

## Decision

- **The document the customer sends is a small model** (`InvoiceSubmissionDto`, v2): the invoice id (uuid), the number (**16 characters**: three letters or digits, the year, nine digits, `^[A-Za-z0-9]{3}20[0-9]{2}[0-9]{9}$`), the date, the currency (TRY), the **customer** (title, VKN/TCKN, tax office, district, city, country) and the lines (description, quantity, unit code, unit price, VAT rate).
  CSV, TXT (`H|`/`L|` lines) and XLSX carry the same columns; an XML upload may be a UBL-TR invoice itself.
- **The supplier is never sent: it is the tenant.** `EdxEInvoiceProfiles` (`Edx.Shared.Helper`) says what each tenant is (VKN, title, tax office, address) and its two aliases, the sender unit (`gb`) and the post box (`pk`). A real integrator keeps this in the customer's account; the sample's three tenants are fixed, like their ids. An uploaded UBL-TR invoice whose supplier is not the tenant is refused.
- **The XML of an invoice is UBL-TR, from the first draft on** (`Edx.Shared.Efatura/Ubl`): `UblInvoiceBuilder` makes the document from the model (tax is rounded per line, totals are the sums), `UblInvoiceReader` summarises a UBL document back (number, uuid, supplier, customer, amounts), and `UblSchemas` validates against the package's schemas (copied beside the library).
  The invoice service validates the draft when it makes it and **again after signing**; a document that does not follow the schema fails the import (stage `Validate`) or the signing, with the first problems in the reason.
- **The signature is a fake of the right shape.** `IInvoiceSigner` (`FakeInvoiceSigner`) puts a XAdES-shaped `ds:Signature` into `ext:UBLExtensions` with made-up values, so the document is what the center wants to see (the center checks that a signature is present, not that it is valid; the sample verifies none). A real signer replaces the interface.
- **The invoice's states grow with delivery:** `Processing`, `Failed`, `DraftCreated`, `Rejected`, `Approved`, `Signed` (as in [ADR-0085](0085-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md)), then `Sent` (the integration service handed the envelope to the center), `Delivered` (the center delivered it to the receiver's post box) and `DeliveryFailed` (the center refused it, or could not deliver it). The queue item is `Completed` when the invoice is delivered and `Failed` (stage `Delivery`) when delivery failed.
- **What the invoice that comes in is:** a **purchase invoice** (`PurchaseInvoice`, in the invoice service: number, uuid, supplier, date, amounts, the XML in the blob store), kept as it came; it is not approved or signed. The amounts are an operation (masked for a role that was not granted it).

## Consequences

- Every sample file, the Python generator, the customer examples and the smoke test use the new columns and the 16-character number; the old `INV-2026-0001` numbers are refused.
- The supplier can no longer be forged by the customer, and an invoice that would be refused by the center is refused before approval.
- Only TRY and the `TEMELFATURA`/`SATIS` invoice are made; other profiles, types and currencies are not (they would extend the builder and the model).
- The schema check costs a little per invoice; the schemas are loaded once.

## Related

[ADR-0085](0085-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0087](0087-the-edx-mock-gib-center.md), [ADR-0089](0089-the-edx-integration-service-sender-unit-and-post-box.md); `samples/edx/shared/Edx.Shared.Efatura/Ubl`, `samples/edx/shared/Edx.Shared.Helper/EdxEInvoiceProfiles.cs`, `samples/edx/service/invoice`.
