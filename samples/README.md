# Samples

There are two sample systems. **Abc** is an event-driven microservice system (an identity service, an auth server, order, product and shipment services, two gateways and two web apps) on the `HsnSoft.Base.*` framework. What references what is in
[abc/REFERENCES.md](abc/REFERENCES.md). **Edx** is a system that takes files from customers (tenants): an identity service (SQL Server), an auth server, a **firm invoice integration** (three hosts, REST, gRPC and WCF: a zip with one invoice file, or a model; checked, kept in MinIO and queued in MongoDB, announced on Kafka), an **invoice service** (takes the invoice in, makes its draft, lets a person approve it and signs it as a UBL-TR e-invoice: [ADR-0033](docs/adr/0033-the-edx-invoice-pipeline-queue-import-draft-approve-sign.md), [ADR-0036](docs/adr/0036-the-edx-invoice-is-a-ubl-tr-document.md)), an **integration service** (the sender unit that hands the signed invoice to the center and the post box that takes in what the center delivers, REST and WCF hosts: [ADR-0037](docs/adr/0037-the-edx-integration-service-sender-unit-and-post-box.md)) a **signer service** (signs the approved invoices; [ADR-0040](docs/adr/0040-the-edx-signer-is-a-service-of-its-own.md)), a **mock GİB center** (a WCF service with the contract of the real one: [edx/mock](edx/mock/README.md), [ADR-0035](docs/adr/0035-the-edx-mock-gib-center.md)) and one gateway ([edx/REFERENCES.md](edx/REFERENCES.md), [ADR-0026](docs/adr/0026-the-edx-sample-a-file-intake-system.md)). The Xyz, Def and Agt samples were removed ([ADR-0025](docs/adr/0025-only-the-abc-sample-stays.md)); the packages they alone used are still covered by the framework's own unit and integration tests, and a product with other choices is composed
from the packages it wants (see [docs/Logging.md](../docs/Logging.md), [framework ADR-0076](../docs/adr/0076-more-providers-and-the-rules-every-provider-shares.md)).

**Layout:** `Samples.sln` (all sample projects; the framework solution does not list them), `docs/` (guides), `docs/adr/` (the decisions about the samples, [index](docs/adr/README.md)), `scripts/` (helpers), `check.sh` (restore, format check, Release build). Rules for working here: [CLAUDE.md](CLAUDE.md).

## Run it

**Edx:** the hosts use SQL Server, MongoDB, Redis, Kafka and MinIO (`localhost:9100`) on their local ports, and the back office (`samples/edx/app/back-office`, Angular 19: `npm install`, `npm start`, then `http://localhost:4200`, which `run-all.sh` also starts) is the screen for entering invoices, the queue, the draft approval, the invoices, the invoices that came in, the envelopes, the reports and the users and roles with their permissions ([ADR-0032](docs/adr/0032-the-edx-back-office-an-angular-app-that-asks-the-services-what-the-user-may-do.md)). MinIO needs a user: `Minio__AccessKey` and `Minio__SecretKey` in the environment or in an ignored `samples/edx/.env`. The keys of the tokens are made first
(`dotnet run samples/scripts/generate-rsa-keys.cs -- samples/edx/app/Edx.AuthServer.Host/private_key.xml samples/edx/shared/Edx.Shared.Hosting/public_key.xml`). `samples/edx/run-all.sh` starts the hosts (the gateway listens on 5600 for the identity, auth and invoice services; the two integrations are called directly: firm invoice REST 5510, gRPC 5511, WCF 5512, GİB invoice 5540, [ADR-0039](docs/adr/0039-the-edx-integrations-are-reached-directly-the-services-through-the-gateway.md)) and `samples/edx/smoke-test.sh` runs what a
customer does and checks how each step ends. Sign in with `POST /auth-server/v1/auth/login` and `{ "tenant": "veriban", "username": "operator", "password": "..." }`; the sample users and their (development) passwords are in `appsettings.development.json` of the identity service: every tenant (`system-tenant`, `veriban`, `bilgera`) has `admin`, `manager`, `operator` and `user` (password `{tenant}-{role}-123`, `system-` for the system tenant). Example files and Python scripts are in [edx/docs](edx/docs), and the programs a customer starts from (REST, gRPC, WCF) in [edx/client](edx/client). What each role may do is a **permission** of one of four types (page, endpoint, operation, constraint; [docs/Authorization.md](../docs/Authorization.md)): the identity service keeps the grants and the menu and the services read the grants from Redis; the starting grants are `DefaultPermissionGrants`, and a tenant's admin changes them with `PUT roles/{id}/permissions` or in the roles page of the back office.

**Abc:**

The hosts use the local infrastructure on its default ports (PostgreSQL, SQL Server, MongoDB, Redis, RabbitMQ). `samples/abc/run-all.sh` builds and starts the hosts of the order flow (Ctrl+C stops them; `PersistentLogger__IsGrayLogActive=true` sends the logs to Graylog) and
**In containers:** `samples/abc/deploy/` has a Dockerfile, a compose file and Kubernetes manifests ([ADR-0047](docs/adr/0047-the-abc-system-runs-in-docker-compose-and-in-kubernetes.md)): `samples/abc/deploy/build-images.sh`, then `docker compose -f samples/abc/deploy/docker-compose.yml --env-file samples/abc/deploy/.env up -d` or `samples/abc/deploy/k8s-up.sh`; `samples/abc/deploy/k8s-test.sh` takes Redis and RabbitMQ away and shows what the probes do ([../docs/Health.md](../docs/Health.md)).
`samples/abc/smoke-test.sh` runs the three outcomes of an order through the commercial gateway and checks how each ends. The keys of the tokens are made first ([The keys of the Abc tokens](#the-keys-of-the-abc-tokens)).

## Logging

Every host logs through `HsnSoft.Base.Logging.Serilog` (`PersistentLogger` section of its `appsettings.json`, see [docs/Logging.md](../docs/Logging.md)). The console gets everything from Debug up; Information and above go to **Graylog** when
`PersistentLogger:IsGrayLogActive` is `true` (GELF HTTP input `127.0.0.1:12202`, the local `infra-graylog`), otherwise to `Logs/<service>.txt`. Every log carries `Solution` (`Abc`: the first part of the assembly name), so in Graylog `Solution:Abc` finds the whole system.

## Abc: the identity service (in the making)

`samples/abc/service/identity` is the start of a new system, laid out as the production identity service is: `Domain` (`UserDomain/{Consts,Entities,Exceptions,Repositories}`),
`Application` (`Contracts/UserDomain/{Dtos,Services}` for the DTOs and the service interface, `Services/` for the implementation), `EntityFrameworkCore`
(`Context`, `Configurations`, `Repositories`) and `Http.Host` (`Controllers`). The host references Application and EntityFrameworkCore, both reference Domain.

The first use case is the CRUD of the users (SQL Server `Abc_Identity_Dev`, created at start; every use is written to a second context, `HistoryDbContext`, on PostgreSQL `Abc_History_Dev`, with the provider, the table prefix, the schema and the connection name in `EfCoreIdentityDbProperties` and `EfCoreHistoryDbProperties`, [framework ADR-0050](../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md)); the controller only calls `IUserAppService`:

| Endpoint | Does |
|---|---|
| `POST /api/identity-service/v1/commercial/users/create` | creates a user (a taken name, in any case: 400 `user_name_duplicate`) |
| `POST /api/identity-service/v1/commercial/users/get/{id}` | one user (404 when there is none) |
| `POST /api/identity-service/v1/commercial/users/paged-list` | a page of the users and how many there are in all: `{ "skip": 0, "take": 20, "userName": "ad", "email": "abc", "isActive": true }` (names and emails by a part of them, in any case) |
| `POST /api/identity-service/v1/commercial/users/filter-list` | all the users that match the same filters, not paged, `limit` at most (1000): `{ "email": "abc", "limit": 50 }` |
| `POST /api/identity-service/v1/commercial/users/search-list` | the users that have the text in their user name or email, shown briefly (`id`, `userName`, `email`), for a box that suggests while one types: `{ "searchText": "ada", "limit": 20 }` |
| `PUT /api/identity-service/v1/commercial/users` | updates the email, the display name and the state (204) |
| `DELETE /api/identity-service/v1/commercial/users/{id}` | deletes (204) |

Every controller derives from `BaseServiceController` (area `identity-service`, `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`), so the endpoints above need a valid
JWT bearer token (no token or a bad one is a 401); only the health endpoints, `PublicUsersController` and the login of the auth server are open. The `Jwt` settings are the Abc ones (sample values, the same in every Abc host that reads a token): a token from `Abc.AuthServer.Host` (`POST http://localhost:5410/api/auth-server/v1/commercial/auth/login`, `alice` / `alice123`, or through the commercial gateway) is accepted; Swagger has an *Authorize* button for it.

`PublicUsersController` (`[AllowAnonymous]`, route `api/identity-service/v1/public/users`) has `POST check-user-name`: whether a user name is free, for what a public sign-up form asks.

```bash
dotnet run --project samples/abc/service/identity/Abc.IdentityService.Http.Host      # http://localhost:5400, Swagger at /swagger in Development
dotnet run --project samples/abc/gateway/Abc.Gateway.Commercial.Host                 # http://localhost:5500
dotnet run --project samples/abc/gateway/Abc.Gateway.Public.Host                     # http://localhost:5600
```

### The Abc gateways

`samples/abc/gateway` has the two gateways of the Abc system, one per audience, built like the production gateway ([ADR-0002](docs/adr/0002-gateways-per-audience.md)):
`Abc.Gateway.Commercial.Host` (port 5500, maps to the `commercial` segment of the services) and `Abc.Gateway.Public.Host` (port 5600, maps to `public`); what they share is `Abc.Shared.Hosting.Gateways` (on top of `Abc.Shared.Hosting`, which the identity service shares through `Abc.Shared.Hosting.Microservices`: [ADR-0003](docs/adr/0003-shared-hosting-projects.md)).
Their routes are settings (`ReverseProxyRoutes`, `ServiceEndpoints`): a client calls `/identity-service/v1/...` and the gateway asks the service `/api/identity-service/v1/commercial/...` (or `public`).

```bash
# the identity service (5400), then a gateway; a token for the commercial one comes from Abc.AuthServer (5410)
curl -X POST http://localhost:5500/identity-service/v1/users/paged-list -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{}'   # commercial: needs the token (the service checks it)
curl -X POST http://localhost:5600/identity-service/v1/users/check-user-name -H 'Content-Type: application/json' -d '{"userName":"ada"}'            # public: open, a PublicUsersController endpoint
```

Each gateway reaches only the endpoints of its own segment (the other one answers 404), and its Swagger UI (`/swagger`) shows the identity service's document with the gateway's paths.

### Entities, DTOs and repositories in Abc

The entities derive from `FullAuditedEntity<Guid>` (and implement `IMultiTenant`), the lists use the framework's DTOs and the generic repository ([framework ADR-0042](../docs/adr/0042-ddd-domain-and-application-packages.md), [docs/DddBuildingBlocks.md](../docs/DddBuildingBlocks.md)). A list is asked for with
`pageNumber` (from 1), `maxResultCount` (10 by default, at most 100), `sortingText` (`"price desc, name"`: only the fields the service offers, `Validation:UnknownSortField` otherwise) and `searchText`, and answers `items`, `dataTotalCount`, `pageTotalCount`,
`resultDataStartNumber`, `resultDataEndNumber`.

### Localization in Abc

The answers are told in the language of the request: the `locale` claim of the token (a user has a `Language`: `admin` is `en`, `alice` is `tr`), else `Accept-Language`, else `en`; the languages are `en`, `tr`, `ru` ([framework ADR-0038](../docs/adr/0038-localization.md),
[docs/Localization.md](../docs/Localization.md)). The texts are in three layers, merged with the innermost first: the framework's, the project `Abc.Shared.Localization` (what several services say, in `en`, `tr`, `ru`: [ADR-0023](docs/adr/0023-the-shared-texts-of-a-product-are-a-project-of-their-own.md)), and `Localization` of each service's domain project (its own errors). A host chooses its localization itself ([ADR-0046](docs/adr/0046-a-host-chooses-its-localization-and-its-cache.md)): the hosting projects know nothing of it. A microservice (and the auth server) makes one call, `AddBaseLocalization(configuration, options => options.AddAbcSharedTexts().AddEmbedded(typeof(Product).Assembly))`, and `app.UseBaseLocalization()` after `UseAbcMicroservice()`; the Domain project of a service references the shared texts, the gateways have no texts of their own and do not choose it. Try it:
`POST /product-service/v1/products/get/{any id}` with alice's token answers in Turkish, with admin's in English (the `Accept-Language` header only counts without the claim); the login of an unknown user with `Accept-Language: ru` answers in Russian. The logs stay in English. The public endpoint `GET /identity-service/v1/users/welcome` (public gateway, no token) greets in the language of the header: `Accept-Language: tr` answers "Hoş geldiniz", `ru` "Добро пожаловать", anything else "Welcome".

### Tenants, audit and soft delete in Abc

Every Abc entity has the audit fields (`CreatedBy`, `UpdatedBy`: the id of the user) and soft delete (a delete marks the row; the lists do not show it); the entities of the identity, order and product services have a tenant too, and **the shipment service has none** (a service takes the tenants it wants, [framework ADR-0074](../docs/adr/0074-multitenancy-is-a-package-of-its-own.md)) ([framework ADR-0037](../docs/adr/0037-audit-soft-delete-and-tenants.md),
[docs/EntityConcepts.md](../docs/EntityConcepts.md)). There are two tenants (`AbcTenants` in `Abc.Shared.Helper`): the **system tenant** (`admin`: sees every tenant) and **Acme** (`alice`: sees only Acme's data, and the two seeded products are Acme's). The auth server puts
`tenant_id` (and `is_system_tenant` for `admin`) in the token. A row made by a user gets the tenant of the token; a user name and a sku are unique in the whole system. A database made before this change must be dropped (new columns): `drop database Abc_Identity_Dev`,
`Abc_Order_Dev`; the MongoDB ones (`Abc_Product_Dev`, `Abc_Shipment_Dev`) too, as their old documents lack the fields.

### The users of Abc

The auth server (which only reads) and the identity service (which owns the database) share one SQL Server database (`ConnectionStrings:EfCore-IdentityService`, `Abc_Identity_Dev`). The identity service creates it on its first start and, when it has no user, adds `admin` / `admin123` and `alice` / `alice123`
(passwords are kept as PBKDF2 hashes). `EnsureCreated` does not change a table that exists: a database made before this change has to be dropped (`drop database Abc_Identity_Dev`). The `CreatedBy` and `UpdatedBy` of a user are the
`sub` of the token of whoever made the request.

### The keys of the Abc tokens

The auth server signs its tokens with an RSA **private** key and every other Abc host checks them with the **public** key ([ADR-0006](docs/adr/0006-jwt-signed-with-an-rsa-private-key.md)); the files are not in git. Make a pair once after a clone:

```bash
dotnet run samples/scripts/generate-rsa-keys.cs -- samples/abc/app/Abc.AuthServer.Host/private_key.xml samples/abc/shared/Abc.Shared.Hosting/public_key.xml
```

The private key stays in `Abc.AuthServer.Host` (and is copied only to its output); the public key is copied next to every host that uses `Abc.Shared.Hosting`. A host whose key file is missing does not start and says which file. Never use the key of
another system here, and never commit a private key.

### The Abc apps

`samples/abc/app` has what people use: the auth server and the two web applications ([ADR-0004](docs/adr/0004-one-auth-server-issues-the-token.md)).

| Project | Is | Port |
|---|---|---|
| `Abc.AuthServer.Host` | a web api: `POST /api/auth-server/v1/commercial/auth/login` (open) gives a JWT access token for a user name and password, `GET .../auth/me` says who the token is. The users are the identity service's, read from its database ([ADR-0007](docs/adr/0007-the-auth-server-reads-the-users-of-the-identity-service.md)); the identity service makes the database and the users `admin` / `admin123` (admin, customer) and `alice` / `alice123` (customer) on its first start; start it before the first login | 5410 |
| `Abc.CommercialWeb.Host` | an MVC application for employees: sign in (the token is kept in the cookie), then a page of the users of the identity service, with a search. It calls **only the commercial gateway** (`http://localhost:5500`) | 5700 |
| `Abc.PublicWeb.Host` | an MVC application for everyone, no sign-in: a form that asks whether a user name is free. It calls **only the public gateway** (`http://localhost:5600`) | 5800 |

```bash
# the order: the identity service, the auth server, the two gateways, then the pages
dotnet run --project samples/abc/app/Abc.AuthServer.Host            # http://localhost:5410
dotnet run --project samples/abc/app/Abc.CommercialWeb.Host         # http://localhost:5700  (sign in as alice / alice123)
dotnet run --project samples/abc/app/Abc.PublicWeb.Host             # http://localhost:5800
```

The commercial gateway also maps `/auth-server/v1/...` to the auth server. Every call a page makes to its gateway carries the correlation id of the page request (and, in the commercial application, the bearer token), so the page, the gateway and
the service behind it are one chain in the logs (the pages and the service share one `CorrelationId` in Graylog; the gateway stays silent for what a service logged). The sign-in of the commercial
application lasts as long as the token (15 minutes); when the token is no longer accepted the page signs out and says so. The web applications answer pages, so they do not use `Abc.Shared.Hosting` (its web api shell answers `BaseResponse`).

### The inbox in Abc ([framework ADR-0067](../docs/adr/0067-the-event-inbox-is-opt-in-and-a-decorator.md))

The identity service (SQL Server), the order service (PostgreSQL) and the product service (MongoDB) have an inbox (`AddEventInbox<...DbContext>()`, or `AddEventInbox("InboxMessage")` on MongoDB; the table or collection `InboxMessage` in their own databases): a message that comes twice is handled once there. The shipment service has none.

### Who references what in Abc ([ADR-0024](docs/adr/0024-who-references-which-shared-project-in-abc.md))

The shared projects build on the framework only; a service takes from them what each of its layers needs, a host adds what it wants for itself (the shared texts, say). Every reference below is read from the project files.

**The shared projects**

| Project | Abc.Shared.* | Own projects | Framework (HsnSoft.Base.*) |
|---|---|---|---|
| `Abc.Shared.Helper` | - | - | `Core` |
| `Abc.Shared.Contracts` | - | - | `EventBus` |
| `Abc.Shared.Localization` | - | - | `Localization` |
| `Abc.Shared.Hosting` | - | - | `AspNetCore.Swashbuckle`, `AspNetCore.WebApi`, `Authentication`, `Logging.Serilog` |
| `Abc.Shared.Hosting.Microservices` | `Hosting` | - | `EventBus.RabbitMQ` |
| `Abc.Shared.Hosting.Gateways` | `Hosting` | - | `AspNetCore.SwaggerGateway` |

**IdentityService**

| Project | Abc.Shared.* | Own projects | Framework (HsnSoft.Base.*) |
|---|---|---|---|
| `Abc.IdentityService.Application` | `Contracts` | `IdentityService.Domain` | `Ddd.Application` |
| `Abc.IdentityService.Domain` | `Helper`, `Localization` | - | `Ddd.Domain`, `MultiTenancy` |
| `Abc.IdentityService.EntityFrameworkCore` | - | `IdentityService.Domain` | `EntityFrameworkCore.PostgreSql`, `EntityFrameworkCore.SqlServer`, `EventBus.EntityFrameworkCore`, `MultiTenancy.EntityFrameworkCore` |
| `Abc.IdentityService.Http.Host` | `Hosting.Microservices` | `IdentityService.Application`, `IdentityService.EntityFrameworkCore` | `Caching.StackExchangeRedis`, `MultiTenancy.Authentication`, `MultiTenancy.EventBus` |

**OrderService**

| Project | Abc.Shared.* | Own projects | Framework (HsnSoft.Base.*) |
|---|---|---|---|
| `Abc.OrderService.Application` | `Contracts` | `OrderService.Domain` | `Ddd.Application` |
| `Abc.OrderService.Domain` | `Helper`, `Localization` | - | `Ddd.Domain`, `MultiTenancy` |
| `Abc.OrderService.EntityFrameworkCore` | - | `OrderService.Domain` | `EntityFrameworkCore.PostgreSql`, `EventBus.EntityFrameworkCore`, `MultiTenancy.EntityFrameworkCore` |
| `Abc.OrderService.Http.Host` | `Hosting.Microservices` | `OrderService.Application`, `OrderService.EntityFrameworkCore` | `Caching.StackExchangeRedis`, `MultiTenancy.Authentication`, `MultiTenancy.EventBus` |

**ProductService**

| Project | Abc.Shared.* | Own projects | Framework (HsnSoft.Base.*) |
|---|---|---|---|
| `Abc.ProductService.Application` | `Contracts` | `ProductService.Domain` | `Ddd.Application` |
| `Abc.ProductService.Domain` | `Helper`, `Localization` | - | `Ddd.Domain`, `MultiTenancy` |
| `Abc.ProductService.Http.Host` | `Hosting.Microservices` | `ProductService.Application`, `ProductService.MongoDB` | `Caching.StackExchangeRedis`, `MultiTenancy.Authentication`, `MultiTenancy.EventBus` |
| `Abc.ProductService.MongoDB` | - | `ProductService.Domain` | `EventBus.MongoDB`, `MongoDB`, `MultiTenancy.MongoDB` |

**ShipmentService**

| Project | Abc.Shared.* | Own projects | Framework (HsnSoft.Base.*) |
|---|---|---|---|
| `Abc.ShipmentService.Application` | `Contracts` | `ShipmentService.Domain` | `Ddd.Application` |
| `Abc.ShipmentService.Domain` | `Helper`, `Localization` | - | `Ddd.Domain` |
| `Abc.ShipmentService.EntityFrameworkCore` | - | `ShipmentService.Domain` | `EntityFrameworkCore.PostgreSql` |
| `Abc.ShipmentService.Http.Host` | `Hosting.Microservices` | `ShipmentService.Application`, `ShipmentService.EntityFrameworkCore`, `ShipmentService.MongoDB` | `Caching.StackExchangeRedis` |
| `Abc.ShipmentService.MongoDB` | - | `ShipmentService.Domain` | `MongoDB` |

**Gateways and apps**

| Project | Abc.Shared.* | Own projects | Framework (HsnSoft.Base.*) |
|---|---|---|---|
| `Abc.Gateway.Commercial.Host` | `Hosting.Gateways` | - | `Caching.StackExchangeRedis` |
| `Abc.Gateway.Public.Host` | `Hosting.Gateways` | - | - |
| `Abc.AuthServer.Host` | `Helper`, `Hosting.Microservices`, `Localization` | `IdentityService.EntityFrameworkCore` | `Caching.StackExchangeRedis`, `MultiTenancy.Authentication` |
| `Abc.CommercialWeb.Host` | - | - | `AspNetCore.WebApi`, `Logging.Serilog` |
| `Abc.PublicWeb.Host` | - | - | `AspNetCore.WebApi`, `Logging.Serilog` |

The web apps (`CommercialWeb`, `PublicWeb`) are pages that call the gateways over HTTP and use no shared Abc project. The reference trees (a microservice, a gateway, the auth server, each shared project) are in [samples/abc/REFERENCES.md](abc/REFERENCES.md).

### Redis and RabbitMQ in Abc

Every Abc microservice (and the auth server) and the commercial gateway are connected to **Redis** (`ConnectionStrings:Redis`, key prefix `abc:`); the four services (identity, order, product, shipment) are also connected to the **RabbitMQ** event bus (exchange `ABC`, the
client name of the service, `RabbitMq` section) ([ADR-0009](docs/adr/0009-redis-and-rabbitmq-in-the-abc-hosts.md)). Both are checked by `/health/ready`, so a host without one is not ready (and `/health/live` stays 200). The infrastructure is the local one on its default ports:
Redis `6379`, RabbitMQ `5672` (`guest` / `guest`). Nothing publishes or consumes an event yet, and nothing reads the cache yet: the connections are what is there.

### The Abc services

`samples/abc/service` has the five-service shape of the system; each microservice is **four projects** ([ADR-0005](docs/adr/0005-microservice-project-pattern.md)): `Abc.<X>Service.Domain`, `.Application`, a persistence project
(`.EntityFrameworkCore` or `.MongoDB`, the only thing that varies) and `.Http.Host`.

| Service | Persistence | Port | State |
|---|---|---|---|
| `identity` | `EntityFrameworkCore` (SQL Server `Abc_Identity_Dev`) | 5400 | users CRUD, a public endpoint |
| `order` | `EntityFrameworkCore` (PostgreSQL `Abc_Order_Dev`) | 5420 | orders with lines, CRUD |
| `product` | `MongoDB` (`Abc_Product_Dev`) | 5430 | products and stock, CRUD; two seeded products (`LAMP-001` with 10 in stock, `CHAIR-001` with 0) |
| `shipment` | `MongoDB` (`Abc_Shipment_Dev`) | 5440 | shipments of an order, CRUD |

Every service has the same endpoints as the users (`create`, `get/{id}`, `paged-list`, `filter-list`, `search-list`, `PUT`, `DELETE`) under `/api/{service}-service/v1/commercial/...`, and the commercial gateway forwards them as
`/order-service/v1/orders/...`, `/product-service/v1/products/...`, `/shipment-service/v1/shipments/...` ([ADR-0008](docs/adr/0008-the-abc-services-crud-pattern.md)). A token is needed; `CreatedBy` / `UpdatedBy` are its `sub`. A database
made before this change must be dropped (`drop database Abc_Order_Dev`); the MongoDB services need nothing.

```bash
dotnet run --project samples/abc/service/order/Abc.OrderService.Http.Host         # http://localhost:5420
dotnet run --project samples/abc/service/product/Abc.ProductService.Http.Host     # http://localhost:5430
dotnet run --project samples/abc/service/shipment/Abc.ShipmentService.Http.Host   # http://localhost:5440
```

### The history of the users (a second context on PostgreSQL)

`HistoryDbContext` (PostgreSQL `Abc_History_Dev`, `ConnectionStrings:EfCore-HistoryService`) keeps what happened to the users: the identity service writes a record for each create, update and delete, and the auth server writes a `login` record for every token it
issues ([ADR-0013](docs/adr/0013-the-history-of-the-users.md)). The records have a CRUD of their own, like the users': `POST /api/identity-service/v1/commercial/user-histories/{get/{id}, paged-list, filter-list, search-list, create}`, `PUT`, `DELETE /{id}`
(`IUserHistoryAppService`; the lists read only the columns of their DTO). Every context of a service has a properties class (`EfCoreIdentityDbProperties`, `EfCoreHistoryDbProperties`, `EfCoreOrderDbProperties`, `MongoProductDbProperties`,
`MongoShipmentDbProperties`) with the table (or collection) prefix, the schema, the provider and the connection name: no setting says which provider runs ([ADR-0012](docs/adr/0012-every-context-is-named-by-its-properties-class.md)).
The connection strings are `EfCore-IdentityService` (SQL Server), `EfCore-HistoryService` (PostgreSQL), `EfCore-OrderService` (PostgreSQL), `Mongo-ProductService` and `Mongo-ShipmentService`.

### Environments ([ADR-0016](docs/adr/0016-environments-of-the-abc-sample.md))

Every host has `appsettings.json` (what is the same everywhere) and `appsettings.development.json`, `.docker.json`, `.uat.json`, `.stage.json`, `.production.json` (what differs: urls, connection strings, RabbitMQ, Graylog, the services behind a gateway). The names are in small letters;
`development` has the values of a developer's machine and is what `dotnet run` and the IDE use (`Properties/launchSettings.json`); the others have fake values (`*.abc.example.com`, `CHANGE_ME`) to be overridden by environment variables or a secret store. The databases are `Abc_<Context>_<Suffix>`
with the suffix `Dev` (development and docker), `Uat`, `Stage` or `Prod`: `Abc_Identity_Dev` (SQL Server), `Abc_History_Dev`, `Abc_Order_Dev`, `Abc_ShipmentHistory_Dev` (PostgreSQL), `Abc_Product_Dev`, `Abc_Stock_Dev`, `Abc_Shipment_Dev` (MongoDB). The tables and the collections are named like their entity, in Pascal case and singular (`User`, `UserHistory`, `Order`, `OrderLine`, `Product`, `ProductStockHistory`, `Shipment`, `ShipmentHistory`, [ADR-0017](docs/adr/0017-tables-and-collections-are-named-like-the-entity.md)); a database made before has to be dropped.

### The order flow ([ADR-0015](docs/adr/0015-the-order-flow-through-events.md), where the events and the handlers live: [ADR-0019](docs/adr/0019-where-events-and-their-handlers-live.md))

A signed-in customer (for example `alice`, of Acme, who gets her token from the auth server) makes an order; the services answer each other through RabbitMQ (exchange `ABC`):

```
order    --OrderCreatedEto-->         product    stock of every line checked and set aside (stock history: "reserved")
product  --OrderStockReservedEto-->   order      Pending -> Confirmed
product  --OrderStockReservedEto-->   shipment   the customer is made when there is none, the payment is taken (Payment collection)
shipment --PaymentProcessedEto-->     shipment   an internal event: paid -> the shipment is made and sent (shipment history: delivered / failed); declined -> nothing is sent
product  --OrderStockRejectedEto-->   order      Cancelled with the reason
shipment --ShipmentCompletedEto-->    order      Confirmed -> Delivered, or Cancelled ("not delivered": payment declined, or the carrier could not reach the address)
```

`POST /api/order-service/v1/commercial/orders/create` with `{ "shippingAddress": "Ankara", "lines": [{ "productId": "...", "productName": "Desk Lamp", "unitPrice": 10, "quantity": 2 }] }`: a few seconds later the order is `Delivered`, the lamp has 2 less
in stock, and there is a `delivered` record in the shipment history. An address with `fail` in it is returned by the (simulated) carrier and the order is cancelled, one with `nopay` has its payment declined and nothing is sent ([ADR-0020](docs/adr/0020-customer-payment-and-an-internal-event-in-the-shipment-service.md); the shipment service has `customers` and `payments` with a CRUD each); an out of stock product (the Office Chair has 0) cancels it at the stock check.
The RabbitMQ management page (`localhost:15672`) shows the queues `ABC_<Service>_<Event>` (`ABC_ProductService_OrderCreated`). The events are in `Abc.Shared.Contracts/Events/<publishing service>/`, the handlers in the Host project of the service that handles them, `EventHandlers/<publishing service>/`. Who made the order (the tenant, the user, the roles) travels in the envelope of every message, and each handler runs as that user: the records the handlers write have `CreatedBy` and `UpdatedBy` ([framework ADR-0057](../docs/adr/0057-the-caller-of-a-message-and-publish-or-send.md)). The order has a `ShippingAddress` now: a database made before this change has to be dropped (`drop database Abc_Order_Dev`).

### More than one store in a service ([ADR-0014](docs/adr/0014-more-than-one-store-in-a-service.md))

| Service | Entity (store) | History (store) | Written when |
|---|---|---|---|
| identity | `User` (EF Core, SQL Server `Abc_Identity_Dev`) | `UserHistory` (EF Core, PostgreSQL `Abc_History_Dev`) | a user is created, changed or deleted; the auth server issues a token (`login`) |
| order | `Order` (EF Core, PostgreSQL `Abc_Order_Dev`) | | |
| product | `Product` (MongoDB `Mongo-ProductService`, `Abc_Product_Dev`) | `ProductStockHistory` (MongoDB `Mongo-StockService`, `Abc_Stock_Dev`, registered under the key `stock`) | a product is created, or an update changes its stock |
| shipment | `Shipment` (MongoDB `Abc_Shipment_Dev`, project `Abc.ShipmentService.MongoDB`) | `ShipmentHistory` (EF Core, PostgreSQL `Abc_ShipmentHistory_Dev`, project `Abc.ShipmentService.EntityFrameworkCore`) | a shipment is delivered (`delivered`) or returned (`failed`) |

Each history has the CRUD of its entity: `POST .../user-histories`, `.../product-stock-histories`, `.../shipment-histories` with `get/{id}`, `paged-list`, `filter-list`, `search-list`, `create`, then `PUT` and `DELETE /{id}`. The connection strings of the new stores are
`Mongo-StockService` and `EfCore-ShipmentHistoryService`.
