# Decisions about the samples

The decisions behind the sample systems, one short file each (why it is the way it is, what was decided, what it costs), in the form of the framework's ADRs ([framework ADR-0001](../../../docs/adr/0001-record-architecture-decisions.md)).
The numbers here are the samples' own; a decision of the framework is written **framework ADR-NNNN** and links to the framework's `docs/adr`. **Every new decision about a sample is added here when it is taken**, in the same change, and the table below is updated.

New ADR: copy the framework's [`0000-template.md`](../../../docs/adr/0000-template.md) to `NNNN-short-title.md` (the next number), fill it in, add a row.

| ADR | Decision | Status | Date |
|---|---|---|---|
| [0046](0046-a-host-chooses-its-localization-and-its-cache.md) | A host chooses its localization and its cache | Accepted | 2026-10-27 |
| [0045](0045-the-commercial-invoice-and-its-answer.md) | The commercial invoice (TICARIFATURA) and the answer of its receiver | Accepted | 2026-10-28 |
| [0044](0044-the-edx-invoice-container-layout.md) | The layout of the storage: a work folder, then everything of a tenant together, and the same folders in both queues | Accepted | 2026-10-28 |
| [0043](0043-the-word-intake-is-firm-invoice.md) | "Intake" is "FirmInvoice" everywhere: routes, contracts, permissions, settings and code | Accepted | 2026-10-28 |
| [0042](0042-the-edx-sales-invoice-and-its-envelope-are-two-statuses.md) | A sales invoice and its envelope have two statuses; the envelope belongs to the GİB invoice integration | Accepted | 2026-10-28 |
| [0041](0041-the-mock-center-envelope-holds-up-to-1000-invoices-and-is-asked-by-uuid.md) | In the mock center an envelope is the post: up to 1000 invoices for one post box; its status is asked by uuid; system envelopes are off | Accepted | 2026-10-28 |
| [0040](0040-the-edx-signer-is-a-service-of-its-own.md) | The Edx signer is a service of its own: `Edx.SignerService.Http.Host` | Accepted | 2026-10-28 |
| [0039](0039-the-edx-integrations-are-reached-directly-the-services-through-the-gateway.md) | The Edx integrations are reached directly; the services are reached through the gateway | Accepted | 2026-10-28 |
| [0038](0038-edx-services-are-named-integration-firm-invoice-and-integration-gib-invoice.md) | The Edx integration services are named by what they integrate: `Edx.Integration.FirmInvoice.*` and `Edx.Integration.GibInvoice.*` | Accepted | 2026-10-28 |
| [0037](0037-the-edx-integration-service-sender-unit-and-post-box.md) | The Edx integration service is the sender unit and the post box of the customers | Accepted | 2026-10-28 |
| [0036](0036-the-edx-invoice-is-a-ubl-tr-document.md) | The Edx invoice is a UBL-TR 1.2.1 document, made from the customer's file and checked against the schema | Accepted | 2026-10-28 |
| [0035](0035-the-edx-mock-gib-center.md) | The Edx mock GİB center: a WCF service with the contract and the checks of the real one | Accepted | 2026-10-28 |
| [0034](0034-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md) | The intake service is three hosts: REST, gRPC and WCF | Accepted | 2026-10-28 |
| [0033](0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md) | The Edx invoice pipeline: queue, import, draft, approve, sign | Accepted | 2026-10-28 |
| [0032](0032-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md) | The Edx back office: an Angular app that asks the services what the user may do | Accepted | 2026-10-28 |
| [0031](0031-the-menu-is-data-in-the-identity-service.md) | The permission tables of the identity service, and the menu as data with its permissions | Accepted | 2026-10-28 |
| [0030](0030-a-new-permission-is-granted-to-the-sample-roles-when-it-is-added.md) | A new permission is granted to the sample roles when it is added | Accepted | 2026-10-27 |
| [0029](0029-the-identity-service-manages-the-permissions.md) | The identity service manages the permissions; the sample roles get their starting grants | Accepted | 2026-10-27 |
| [0028](0028-edx-identity-tenants-roles-and-signing-in-by-tenant.md) | The Edx identity service: tenants, roles, and signing in by the code of the tenant | Accepted | 2026-10-27 |
| [0027](0027-grpc-and-wcf-are-entry-points-of-a-host.md) | gRPC and WCF are entry points of a host, not framework packages | Accepted (the placement in the `Http.Host` is superseded by [ADR-0034](0034-the-intake-service-is-three-hosts-rest-grpc-and-wcf.md)) | 2026-10-27 |
| [0026](0026-the-edx-sample-a-file-intake-system.md) | The Edx sample: a system that takes files from customers | Accepted | 2026-10-27 |
| [0025](0025-only-the-abc-sample-stays.md) | Only the Abc sample stays | Accepted | 2026-10-27 |
| [0024](0024-who-references-which-shared-project-in-abc.md) | Who references which shared project in Abc | Accepted | 2026-10-26 |
| [0023](0023-the-shared-texts-of-a-product-are-a-project-of-their-own.md) | The shared texts of a product are a project of their own | Accepted | 2026-10-26 |
| [0022](0022-the-stock-is-changed-in-one-step.md) | The stock is changed in one step of the store | Accepted | 2026-10-26 |
| [0021](0021-the-xyz-sample-beside-abc.md) | The Xyz sample beside Abc | Superseded by [ADR-0025](0025-only-the-abc-sample-stays.md) (the Xyz sample was removed) | 2026-10-25 |
| [0020](0020-customer-payment-and-an-internal-event-in-the-shipment-service.md) | Customer, payment and an internal event in the shipment service | Accepted | 2026-10-25 |
| [0019](0019-where-events-and-their-handlers-live.md) | Where the events and their handlers live | Accepted | 2026-10-25 |
| [0018](0018-names-on-the-broker-in-the-abc-sample.md) | The names on the broker in the Abc sample | Accepted | 2026-10-25 |
| [0017](0017-tables-and-collections-are-named-like-the-entity.md) | Tables and collections are named like the entity | Accepted | 2026-10-25 |
| [0016](0016-environments-of-the-abc-sample.md) | The environments of the Abc sample, and the names of its databases | Accepted | 2026-10-25 |
| [0015](0015-the-order-flow-through-events.md) | The order flow runs through events: order, product, shipment, order | Accepted (the events and the tenant handling changed: [framework ADR-0057](../../../docs/adr/0057-the-caller-of-a-message-and-publish-or-send.md); the names on the broker: [ADR-0018](0018-names-on-the-broker-in-the-abc-sample.md); where they live: [ADR-0019](0019-where-events-and-their-handlers-live.md)) | 2026-10-25 |
| [0014](0014-more-than-one-store-in-a-service.md) | More than one store in a service: a second MongoDB under a key, a relational project beside a MongoDB one | Accepted | 2026-10-25 |
| [0013](0013-the-history-of-the-users.md) | The history of the users: a CRUD of its own, written by the identity service and the auth server | Accepted | 2026-10-25 |
| [0012](0012-every-context-is-named-by-its-properties-class.md) | Every context of a sample is named by its properties class; the provider setting is gone | Accepted | 2026-10-25 |
| [0011](0011-abc-databases-and-provider-references.md) | The Abc services' databases, and a project references only the provider it uses | Accepted (the settings of the identity databases changed: [framework ADR-0050](../../../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md)) | 2026-10-25 |
| [0010](0010-the-persistence-project-registers-its-database.md) | The persistence project registers its database provider | Accepted | 2026-10-25 |
| [0009](0009-redis-and-rabbitmq-in-the-abc-hosts.md) | Redis and RabbitMQ in the Abc hosts | Accepted | 2026-10-25 |
| [0008](0008-the-abc-services-crud-pattern.md) | The CRUD pattern of the Abc services: order, product and shipment follow the identity service | Accepted (the per-service `ICurrentUser` is replaced by the framework's, [framework ADR-0036](../../../docs/adr/0036-icurrentuser-in-the-framework.md); the audit fields, soft delete and tenants are the framework's, [framework ADR-0037](../../../docs/adr/0037-audit-soft-delete-and-tenants.md)) | 2026-10-25 |
| [0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md) | The auth server reads the users of the identity service; the author of a row is the `sub` of the token | Accepted (`CreatedBy`/`UpdatedBy` are now the id of the user, and the token has tenant claims, [framework ADR-0037](../../../docs/adr/0037-audit-soft-delete-and-tenants.md); the database is SQL Server now, [ADR-0011](0011-abc-databases-and-provider-references.md)) | 2026-10-25 |
| [0006](0006-jwt-signed-with-an-rsa-private-key.md) | The JWT is signed with an RSA private key; the other hosts only have the public key | Accepted | 2026-10-25 |
| [0005](0005-microservice-project-pattern.md) | Every microservice has the same four projects; only the persistence project varies | Accepted (the domain project depends on Core and `Ddd.Domain`, the application project on `Ddd.Application`: [framework ADR-0042](../../../docs/adr/0042-ddd-domain-and-application-packages.md)) (the host no longer picks the database provider: the persistence project registers it, [ADR-0010](0010-the-persistence-project-registers-its-database.md)) | 2026-10-25 |
| [0004](0004-one-auth-server-issues-the-token.md) | One auth server issues the JWT; every other host only validates it | Accepted (the shared signing key it describes is replaced by an RSA key pair, [ADR-0006](0006-jwt-signed-with-an-rsa-private-key.md); the user list of its settings by the users of the identity service, [ADR-0007](0007-the-auth-server-reads-the-users-of-the-identity-service.md)) | 2026-10-25 |
| [0003](0003-shared-hosting-projects.md) | A product's hosts share their composition in `{Product}.Shared.Hosting`, with one specialization per kind of host | Accepted | 2026-10-25 |
| [0002](0002-gateways-per-audience.md) | One gateway per audience, built from a route registry; the gateway forwards, the services decide who may call | Accepted | 2026-10-25 |
| [0001](0001-the-samples-are-self-contained.md) | The samples are self-contained: their own solution, documents and decisions | Accepted | 2026-10-28 |
