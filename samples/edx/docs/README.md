# Edx: example files and scripts

What a customer needs to send an invoice to Edx: the formats of the file, examples of each, and small Python scripts (standard library only) to try the REST way in. The programs to start from in C# (REST, gRPC, WCF) are in [../client](../client).

## What is sent

**A zip with exactly one file**, whatever the way in (REST, gRPC, WCF; the back office's upload page too), with four things said about it:

| | |
|---|---|
| `invoiceId` | the **uuid** of the invoice, made by the sender; it stays the same when the invoice is sent again |
| `invoiceNumber` | the **number** of the invoice; the file says the same number |
| `sourceType` | the type of the file in the zip: `Xml`, `Csv`, `Txt` or `Xls` (an `.xlsx` is `Xls` too); it must be the file's extension |
| `fileHash` | the **SHA-256 of the zip**, in hex; the service checks it |

The service answers **queued** (201) or why it refused at once (400 with a code: `upload_hash_mismatch`, `upload_not_a_zip`, `upload_not_one_file`, `upload_type_mismatch`, `upload_xml_not_well_formed`, `upload_too_large`, ...). What became of the invoice is read from the queue
(`POST /api/firm-invoice-service/v1/commercial/queue/paged-list` on the firm invoice integration (`http://localhost:5510`), `.../queue/by-invoice/{invoiceId}`, or the queue page of the back office): `Queued`, then `DraftCreated` (a person approves or rejects it), `Completed` (approved, signed), or `Failed` / `Rejected` with the reason.

An invoice that waits for approval, or was approved or signed, **cannot be sent again** (use another number; reject the draft first to send it again). One that failed or was rejected is sent again with the **same uuid and number**, corrected: the new one replaces it.

A **model** (no file) is the same invoice as JSON or XML: `POST /api/firm-invoice-service/v1/commercial/models/invoice` (`http://localhost:5510`) (`?format=xml` keeps it as an XML file), the gRPC `SubmitInvoice` or the WCF `SubmitInvoice`.

## The formats of the file

All of them say: the number, the issue date (`yyyy-MM-dd`), the currency (three letters), the **customer** (name, tax id (VKN/TCKN), tax office, district, city, country) and **one or more lines** (description, quantity, unit code, unit price, VAT rate in percent). The number has 16 characters (three letters or digits, the year, nine digits: `ABC2026000000001`). **The supplier is not sent: it is the tenant** of the user who sends the invoice ([ADR-0036](../../docs/adr/0036-the-edx-invoice-is-a-ubl-tr-document.md)). Decimals have a dot. Samples are in [samples](samples), their zips in [zips](zips).

**CSV** ([invoice.csv](samples/invoice.csv); [invoice-commercial.csv](samples/invoice-commercial.csv) is a commercial invoice, which the receiver answers): a header row with the columns `invoiceNumber, issueDate, currency, customerName, customerTaxId, customerTaxOffice, customerDistrict, customerCity, customerCountry, description, quantity, unitCode, unitPrice, vatRate` (any order, any case; the optional `invoiceProfile` is `TEMELFATURA`, the default, or `TICARIFATURA`: a commercial invoice, which its receiver answers: [ADR-0045](../../docs/adr/0045-the-commercial-invoice-and-its-answer.md)), then **a row for each line**. The first row says the header of the
invoice; the next rows may leave those columns empty, and may not say another value.

**XLS / XLSX** ([invoice.xlsx](samples/invoice.xlsx)): the first sheet, the same columns as the CSV. (An `.xls`, the old binary format, is read too; save the xlsx as "Excel 97-2003" to get one.)

**TXT** ([invoice.txt](samples/invoice.txt)): one `H` line and a `L` line for each line of the invoice, parts separated by `|`; empty lines and lines that start with `#` are left.
`H|number|date|currency|customer|customerTaxId|taxOffice|district|city|country[|invoiceProfile]`, `L|description|quantity|unitCode|unitPrice|vatRate`.

**XML** ([invoice.xml](samples/invoice.xml)): a **UBL-TR 1.2.1 invoice** (UBL 2.1, `TEMELFATURA`), checked against the schema of the package (`einvoice/UBLTR_1.2.1_Paketi/xsdrt`, main document `UBL-Invoice-2.1.xsd`) before it is read; its `cbc:ID` and `cbc:UUID` must be the number and the uuid sent with it, and its supplier must be the tenant (the sample is Veriban's).

**Model JSON** ([invoice-model.json](samples/invoice-model.json)): the same fields in camel case, with `invoiceId` and `invoiceNumber`.

Other samples: [invalid-missing-column.csv](samples/invalid-missing-column.csv) and [invalid-bad-number.csv](samples/invalid-bad-number.csv) fail at the stage `Validate` (they say what is wrong), [over-limit.csv](samples/over-limit.csv) amounts to more than an operator may send (stage `Limit`). The number and the uuid inside a sample are
replaced by the ones you send it with (the smoke test does so); to send a sample as it is, send it with the number `ABC2026000000001` (and, for the XML, the uuid in the file).

## The scripts ([python](python))

```bash
python3 python/make_samples.py        # makes samples/invoice.xlsx and a zip of every sample (zips/)

export EDX_TENANT=veriban EDX_USER=operator EDX_PASSWORD=veriban-operator-123     # the development users of the identity service
python3 python/edx_client.py upload samples/invoice.csv ABC2026000000001             # zips, hashes and uploads; a new uuid is made and shown
python3 python/edx_client.py queue <the uuid>                                     # what became of it
```

`edx_client.py` is also a module: `login`, `zip_bytes`, `sha256`, `upload` (a multipart form: the zip and its four fields) and `queue_of` are what a customer's program does. The invoice number of the file and the one sent with it must be the same, so the example above is accepted
only for a file that says `ABC2026000000001`. `samples/edx/smoke-test.sh` runs the whole pipeline with these files.
