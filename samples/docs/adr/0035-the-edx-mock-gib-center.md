# ADR-0035: The Edx mock GİB center: a WCF service with the contract and the checks of the real one

- **Status:** Accepted
- **Date:** 2026-10-28

## Context

An invoice of an Edx customer (Veriban, Bilgera) is made to be received by somebody: the receiver may be a customer of this integrator or of another one. In the Turkish e-invoice system the integrators do not send to each other: the sender's integrator (its **sender unit**, *gönderici birim*) hands the signed, enveloped invoice to the
**center** (GİB), which checks it, tells the sender what it found, finds the receiver's **post box** (*posta kutusu*: an address, an **alias**, that belongs to the integrator the receiver is with) and delivers the envelope there; the post box answers the center, and the center passes the answer on to the sender. Edx needs a stand-in for the
center to be developed and tested against. The guides are in `samples/edx/docs/einvoice` (the integration guide v1.10, Ek-1 envelope, Ek-2 system response, Ek-3 software standards and objects, the private integration guide v1.14, the UBL-TR 1.2.1 package with its schemas and samples); an earlier attempt
(`eds-mock-gib-center-invoice`) had the contract and the MTOM binding.

## Decision

**The mock center is a host of its own in the Edx sample** (`samples/edx/mock`: `Edx.Mock.Center.Domain`, `.MongoDB`, `.Wcf.Host`), over a shared project `Edx.Shared.Efatura` that the center and the integrator side (a sender unit, a post box) both use. Only the **e-invoice** is handled: invoices (`SENDERENVELOPE`), application
responses (`POSTBOXENVELOPE`) and system responses (`SYSTEMENVELOPE`); the other documents of the family (despatch, receipt advice) and the user envelopes are refused.

- **The contract is the real one** (WSDL `EFaturaPortType`, namespace `http://gib.gov.tr/vedop3/eFatura`): `sendDocument` (the envelope as a zip named by its uuid, as an **MTOM** attachment, with its **MD5**) and `getApplicationResponse` (the status of an envelope, as a system envelope), `BasicHttpBinding` with MTOM, XmlSerializer bodies,
  the fault `EFaturaFault` (code and message) with the codes 2000 to 2007 of Ek-3. The project has the contract twice because the two stacks have different attributes: for CoreWCF (what a host serves, `IEFaturaPortType`) and for System.ServiceModel (what a client calls, `EFaturaClient`, which returns a result and never throws for a refusal or an unreachable service).
  An integrator's post box serves the same contract (and the center asks it with `getApplicationResponse`).
- **An envelope** is as Ek-1 says: a zip with one file, `{uuid}.xml`, a `StandardBusinessDocument` (header version 1.0; sender and receiver with their alias and `VKN_TCKN`/`UNVAN` contacts; the document identification with the standard `UBL-TR`, the type version `1.2` (UBL 2.1), the uuid, the type and the time; the package `ef:Package` with `Elements`: `ElementType`,
  `ElementCount` and the `ElementList` of documents). A system response is as Ek-2 says: an `ApplicationResponse` with `SenderParty`, `ReceiverParty`, `DocumentResponse` (`S_APR`), the `DocumentReference` of the envelope and a `LineResponse` with the status code and text.
- **The work is a queue with workers** (the answer of `sendDocument` is fast; the rest is asynchronous), and the status codes are those of Ek-2:
  1. `sendDocument` checks the name (a uuid and a zip, 2006), the MD5 (2000) and that the uuid is new (2001), keeps the zip in the blob store (2002) and queues the envelope (1000, or 2003).
  2. The **analysis** (1100) does what the guide says of the center: the zip holds one XML (1110 to 1132), the envelope is of the versions the center knows and its uuid is the name of the file and of the zip (1111, 1133, 1140 to 1143), the sender and the receiver are registered (1170) and both addresses are said (1180), the sender may send
     from its alias (a sender unit for an invoice, 1171; a post box for an application response, 1172) and the receiver has the address (1181), the documents are of the kind the envelope says and follow their **UBL-TR XML schema** (1160, the schemas of the package), they **carry a signature** (present, 1161) and an invoice was not sent before (1163: the same number from the same sender, or the same uuid, in an envelope
     the center still holds; a refused envelope, 1215 and 1230 let it be sent again). **Not done:** the verification of the signature (1175 to 1177: the center does not do it either, the sender unit and the post box do), the schematron rules (the package has none), the signature owner against the sender, and the user envelopes.
  3. The **preparation** makes the **system response to the sender** (1200 or the refusal) and delivers an envelope that was taken, **as it came**, to the address of the receiver. A **system envelope** from a post box or a sender unit changes the status of the envelope it is about (1200 means it took it in: 1300; else 1230) and is **forwarded as it is** to the address it is for.
  4. The **delivery** sends the zip with the same `sendDocument` to the endpoint the receiver's alias names: taken is 1220 (waiting for the answer), not reachable is 1210 and tried again after a wait (the GİB center: four retries, two hours apart; here the settings), then 1215; a refusal is not tried again. An envelope still 1220 after a while is **asked about** (`getApplicationResponse` of the post box) and its answer is applied and forwarded as if the post box had sent it.
- **Who is registered** is data in MongoDB, made from the settings at every start (`Participants`): a register number (VKN), a title and the aliases with the endpoint that serves each (the integrator's service, for a post box and for a sender unit that receives system responses). The center is a participant too. In the real system an integrator registers its customers with a
  user envelope; the mock reads them from its settings. Veriban and Bilgera have a sender unit and a post box each, served by the post box of Edx; a customer of "another integrator" points at a service that is not there.

## Consequences

- The invoice side can be built against something that behaves like the center, including its failures: a refused envelope, a duplicate invoice, a post box that is down or silent.
- Edx now needs, on its side, **a post box WCF service** (what the center delivers to: it checks the envelope, answers with a system response) and a **sender unit** that signs the invoice (UBL-TR, XAdES), envelopes it and hands it to the center; the contract, the envelope code, the schemas and the system response are in `Edx.Shared.Efatura`. The invoice of the sample (`InvoiceSubmissionDto`) is not UBL-TR: it has to
  become a UBL-TR invoice before it can be sent.
- `samples/edx/mock/tools` has a stub post box and a sender unit (file-based programs) to try the center with until then.
- The center keeps what it was handed in its own MinIO container (`edx-mock-center`) and its own database; it is not a microservice of Edx (no token, no events). The system responses it makes have a placeholder address in their parties (the schema asks for one; the mock does not know the real one).

## Related

[ADR-0027](0027-grpc-and-wcf-are-entry-points-of-a-host.md) (WCF in the samples), [ADR-0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md); `samples/edx/mock/README.md`, `samples/edx/docs/einvoice`.
