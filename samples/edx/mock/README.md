# The mock GİB center

A stand-in for the e-invoice center ([ADR-0035](../../docs/adr/0035-the-edx-mock-gib-center.md)); the guides it follows are in [../docs/einvoice](../docs/einvoice). Only the **e-invoice** is handled (invoice, application response, system response).

```
sender unit ── 1 SENDERENVELOPE (invoice) ──▶ CENTER ── 3 SENDERENVELOPE, as it came ──▶ post box
     ▲                                          │ ▲                                         │
     ├──────── 2 SYSTEMENVELOPE (what the      ◀┘ └──── 4 SYSTEMENVELOPE (took it in?) ◀───┘
     │            analysis found) ◀──────────────┤
     └──────── 5 SYSTEMENVELOPE of the post box, forwarded as it is ◀──┘
     getApplicationResponse: what became of an envelope (1000 … 1300)
```

| | |
|---|---|
| Service | `http://localhost:5700/EFaturaMerkez/services/EFatura/` (`?wsdl`), `Edx.Mock.Center.Wcf.Host`, `BasicHttpBinding` with MTOM |
| Contract | `sendDocument` (the `{uuid}.zip` and its MD5), `getApplicationResponse` (`Edx.Shared.Efatura`: the server contract for CoreWCF, `EFaturaClient` for calling it) |
| Data | MongoDB `Edx_MockCenter_Dev` (participants, the envelope queue), MinIO container `edx-mock-center` (the zips) |
| Settings | `Center` (who it is, the workers, the retries, the schemas, whether a signature is required), `Participants` (who is registered, with their aliases and the endpoints that serve them) |
| Schemas | the UBL-TR 1.2.1 `xsdrt` of the package in `../docs/einvoice` (linked into the host as `Schemas/`) |

## What it checks (analysis)

The zip holds one XML · the envelope is of the versions of the guides (header 1.0, `UBL-TR`, type version 1.2) and its uuid is the name of the file and of the zip · sender and receiver are registered, both addresses are said · the sender sends from a sender unit (invoice) or a post box (application response) it
has, the receiver has the address · the documents are of the kind the envelope says and **follow the UBL-TR schema** · they **carry a signature** (its presence; **the signature is not verified**, the real center does not either) · the invoice was not sent before (the same number from the same sender, or the same uuid, in an envelope the
center still holds). **Not done:** schematron rules (not in the package), verification of the signature, user envelopes, the other documents of the e-invoice family.

**An envelope is the post:** it holds up to 1000 documents (`Center:MaxDocumentsPerEnvelope`), all for the post box it is addressed to (an invoice for another customer is refused, 1181). The status is kept on the envelope, not on its invoices, and is asked for with the uuid of the envelope (`getApplicationResponse`). **System envelopes are off** by default (`Center:SendSystemResponses`): the sender
asks, and the center asks the post box for its answer ([ADR-0041](../../docs/adr/0041-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md)).

## Who is registered (settings `Participants`)

| VKN | Participant | Aliases | Served by |
|---|---|---|---|
| `1234567801` | Veriban | `urn:mail:veriban_gb@veriban.example` (sender unit), `urn:mail:veriban_pk@veriban.example` (post box) | the post box of Edx (the WCF host of the integration service, [ADR-0037](../../docs/adr/0037-the-edx-integration-service-sender-unit-and-post-box.md)), `http://localhost:5541/EFatura/` |
| `1234567802` | Bilgera | `urn:mail:bilgera_gb@bilgera.example`, `urn:mail:bilgera_pk@bilgera.example` | the same |
| `1234567803` | a customer of another integrator | `urn:mail:diger_pk@diger.example` | `http://localhost:5800/EFatura/` (not there: the delivery is tried again) |
| `9999999999` | the center | `urn:mail:defaultgb@merkez.efatura.mock` | itself |

The **user list** (who is registered, with the aliases) is served as the real center does, `GET /newUserPkListxml_full.zip` (post boxes) and `GET /newUserGbListxml_full.zip` (sender units); the integration service reads it at start and refreshes it.

## What the codes say

`1000` queued · `1100` being analyzed · `1110`…`1181` refused (zip, XML, header, authority, address, schema `1160`, no signature `1161`, invoice sent before `1163`) · `1200` taken · `1210` could not be delivered, tried again · `1215` given up · `1220` delivered, no answer yet ·
`1230` the post box refused it · `1300` done. A call that is refused before queueing is a fault: `2000` the MD5, `2001` the uuid is there, `2004` no such envelope, `2006` the name.

## Trying it

```bash
# the center (it needs MongoDB and MinIO; Minio__AccessKey and Minio__SecretKey in the environment), also started by samples/edx/run-all.sh
ASPNETCORE_ENVIRONMENT=development dotnet run --project samples/edx/mock/Edx.Mock.Center.Wcf.Host --no-launch-profile

cd samples/edx/mock/tools
dotnet run stub-postbox.cs                 # a post box on :5541 that takes what the center delivers and answers it (SILENT=1: the center has to ask)
dotnet run send-batch.cs -- 1000 ok            # one envelope with 1000 invoices for one post box (1001, or `5 mixed` for a wrong receiver, are refused: [ADR-0041](../../docs/adr/0041-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md))
dotnet run send-invoice.cs                 # a sender unit: sends a sample invoice of the UBL-TR package and follows its status  (… -- - - - nosig | badxml: a refused one)
```
