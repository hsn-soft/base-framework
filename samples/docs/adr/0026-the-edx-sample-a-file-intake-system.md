# ADR-0078: The Edx sample: a system that takes files from customers

- **Status:** Accepted
- **Date:** 2026-10-27 (adds a second sample to [ADR-0077](0077-only-the-abc-sample-stays.md), which kept only Abc, "other samples are added on purpose")

## Context

Abc is an order flow on RabbitMQ with SQL and MongoDB. A new product was asked for with other needs: customers (tenants) send files in several shapes (zips of XML, zips of text/CSV/Excel, a typed request model), through the protocol each one can speak (REST, gRPC, WCF), the files are kept in
an object store whose place can change, every record written is announced on **Kafka**, and a second service keeps a few facts about each file. The system has tenants, Redis for the hosts that need it, Serilog and a JWT. It is also the test of the claim that a product picks its own capabilities from the
framework: what Abc composes (RabbitMQ, localization, a relational store behind every service) is not what this system composes.

## Decision

`samples/edx` is a second sample, laid out like Abc ([ADR-0028](0028-shared-hosting-projects.md), [ADR-0030](0030-microservice-project-pattern.md)): `shared`, `app`, `gateway`, `service/{identity,intake,invoice}`.

- **Hosts and what each reaches.** Gateway (YARP; Redis), auth server (Redis, the identity database), identity service (SQL Server, [ADR-0080](0080-edx-identity-tenants-roles-and-signing-in-by-tenant.md)), **intake service** (MongoDB, MinIO, Kafka, Redis, REST + gRPC + WCF), **invoice service** (MongoDB, Kafka, Redis).
  **Only the microservices reach Kafka**: `Edx.Shared.Hosting.Microservices` has the Kafka event bus, and the gateway and the auth server are built on `Edx.Shared.Hosting` / `Edx.Shared.Hosting.Gateways`, which do not reference a broker. The identity service is a microservice that has no events, so it does not call
  `AddEdxEventBus`. Redis is chosen by the host (`AddBaseRedis`) in the gateway, the auth server and every microservice. No host chooses localization: the system has no texts in several languages.
- **One intake service, not two.** Receiving a zip of XML, a zip of tables and a request model is one job with one storage rule (the folders, the blob, the record, the event); two services would copy it or need a shared project for it ([ADR-0005](0005-where-code-lives.md): code that only one service uses stays in it). The three
  protocols are entry points of this one service ([ADR-0079](0079-grpc-and-wcf-are-entry-points-of-a-host.md)).
- **Where the bytes go is a setting; how they are organised is data.** `Storage:Container` and `Storage:RootPath` say where the blobs are written (`IBlobStore`; MinIO today, any other store by changing one registration and these two values). The hierarchy `{tenant}/{yyyy}/{MM}/{dd}/{HH}/{mm}` is kept **in MongoDB**, a
  `StorageFolder` record for each level (unique by tenant and path, so two uploads of one minute make the folder once), and each `StoredFile` record keeps the folder, the container and the object name it was written with, so a file stays readable after the settings move.
- **Every record written is announced.** `StorageFolderCreatedEto` for each new folder and `FileStoredEto` for each file, published by the intake service. The invoice service handles `FileStoredEto` (with the inbox, [ADR-0067](0067-the-event-inbox-is-opt-in-and-a-decorator.md)) and keeps a few facts in its own database; it has no use for the folder
  events and does not subscribe. There is no outbox ([ADR-0057](0057-the-caller-of-a-message-and-publish-or-send.md), to be discussed): when the broker is down after the record was written, the caller is told and the file stays recorded.
- **What an upload is checked for**, before anything is kept (`ArchiveInspector`): a zip, not empty, at most `Intake:MaxArchiveEntries` files, at most `Intake:MaxUnpackedBytes` unpacked, no entry name that is rooted or climbs out (`..`), only `.xml` files that are well formed (no DTD, no external entity) in an XML archive, only the
  `Intake:TabularExtensions` in a table archive; the size of the upload is limited while it is copied, whatever the sender says. The archive is stored as it came; nothing is unpacked.
- **One request model for all protocols.** `InvoiceSubmissionDto` is JSON by name and WCF by `[DataContract]`; the gRPC message is the same fields. The service keeps it as a JSON file or an XML file (the XML is the data contract's, the one a WCF client puts on the wire).
- **The shared gateway code is copied, not shared with Abc.** `Edx.Shared.Hosting.Gateways` is Abc's route registry with one addition (`Http2` on a route, for the gRPC cluster). Two products now carry the same code; ADR-0005 says it moves to the framework when a second product wants it unchanged. This is that second product, and the move is not done in this change
  (a package with its tests and a change to Abc): it is an open item.

## Consequences

- The sample folder has two systems; a framework change is carried into both ([samples/README.md](../../samples/README.md), [samples/edx/REFERENCES.md](../../samples/edx/REFERENCES.md)). `samples/edx/run-all.sh` and `smoke-test.sh` do for Edx what they do for Abc. MinIO needs a user, given by `Minio__AccessKey` / `Minio__SecretKey` in the environment or in
  an ignored `samples/edx/.env`; no value is in a file.
- A WCF client can send what one SOAP message may weigh (`Intake:MaxUploadBytes`, base64 in the XML, buffered in memory); the bigger archives go over REST or gRPC, which stream.
- The tests of the sample are written with the other sample tests, after the framework is done; the smoke test and a live run against the real infrastructure are what checks it for now.

## Related

[ADR-0005](0005-where-code-lives.md), [ADR-0026](0026-route-pattern-and-jwt-by-default.md), [ADR-0028](0028-shared-hosting-projects.md), [ADR-0074](0074-multitenancy-is-a-package-of-its-own.md), [ADR-0077](0077-only-the-abc-sample-stays.md), [ADR-0079](0079-grpc-and-wcf-are-entry-points-of-a-host.md), [ADR-0080](0080-edx-identity-tenants-roles-and-signing-in-by-tenant.md).
