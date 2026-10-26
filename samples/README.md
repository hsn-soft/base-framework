# Xyz: the second system ([ADR-0061](../docs/adr/0061-the-xyz-sample-beside-abc.md))

A second event-driven microservice system on the `HsnSoft.Base.*` framework, written **beside Abc** to do the same job (users, products, orders, shipments, customers, payments, histories, the same order flow) with other choices,
so the two show that the framework fits more than one product. What differs:

| | Abc | Xyz |
|---|---|---|
| Users | an identity service (CRUD) and an auth server that reads its database | **one auth server**: it keeps the users and their history and issues the tokens |
| Gateways | commercial and public (and two web apps) | **one commercial gateway** (with a Redis rate limit) |
| Layers of a service | class libraries (Domain, Application, EntityFrameworkCore/MongoDB, Http.Host) | **folders of the Host project** (`Domain`, `Application`, `Infrastructure`, `Controllers`, `EventHandlers`) |
| Databases | SQL Server, PostgreSQL, MongoDB | **PostgreSQL only** (a service with two stores has two contexts on two databases) |
| Message broker | RabbitMQ (exchange `ABC`) | **Kafka** (topics `XYZ_<Event>`, one consumer group per service and event, `XYZ_<Service>_<Event>`) |
| Redis | rate limit (gateway), refresh tokens | the same: rate limit in the gateway (`Xyz.Shared/RateLimiting`), refresh tokens in the auth server |
| Shared code | several `Abc.Shared.*` projects | one project, `Xyz.Shared` (hosting, gateway shell, event contracts, rate limit) |

```
 client --> Xyz.Gateway.Commercial (5100) --> Xyz.AuthServer (5110)      users, user history, tokens (RS256), refresh tokens (Redis)
                |   rate limit (Redis)  \--> Xyz.OrderService (5120)     orders                      --OrderCreatedEto-->
                                         \--> Xyz.ProductService (5130)  products, stock history     <--OrderStockReserved/Rejected--
                                          \-> Xyz.ShipmentService (5140) shipments, customers, payments, shipment history --ShipmentCompletedEto--> order
```

| Host | Port | Databases (PostgreSQL) | Notes |
|---|---|---|---|
| `Xyz.Gateway.Commercial.Host` | 5100 | - (Redis) | `/<service>/v1/...` is `/api/<service>/v1/commercial/...` of the service; routes are settings (`ReverseProxyRoutes`, `ServiceEndpoints`) |
| `Xyz.AuthServer.Host` | 5110 | `Xyz_AuthServer_<E>`, `Xyz_AuthServerHistory_<E>` | `auth/login`, `auth/refresh`, `auth/logout`, `auth/me`; users CRUD; every token issued is written to the history as the user who signs in |
| `Xyz.OrderService.Host` | 5120 | `Xyz_Order_<E>` | orders; handles the stock and shipment events |
| `Xyz.ProductService.Host` | 5130 | `Xyz_Product_<E>`, `Xyz_Stock_<E>` | products; the stock history is a second context on its own database |
| `Xyz.ShipmentService.Host` | 5140 | `Xyz_Shipment_<E>`, `Xyz_ShipmentHistory_<E>` | shipments, customers, payments; the payment and the parcel are two steps joined by an internal event |
| `Xyz.Shared` | - | - | what the hosts share |

`<E>` is `Dev`, `Stage`, `Uat` or `Prod`. The environments are the same five as in Abc (`development`, `docker`, `stage`, `uat`, `production`: `appsettings.<environment>.json` next to `appsettings.json`); only `development` has working values
(local PostgreSQL `postgres`/`postgres`, Redis and Kafka on `localhost`), the others have example host names and `CHANGE_ME` passwords: real ones come from the environment or a secret store, never from a file in git.
A context names its provider and connection string in code (`EfCore<Name>DbProperties`: `EfCoreProviderNames.Postgres`, `EfCore-OrderService`, ...), not in settings.

## The order flow

The same as in Abc ([ADR-0054](../docs/adr/0054-the-order-flow-through-events.md), [ADR-0060](../docs/adr/0060-customer-payment-and-an-internal-event-in-the-shipment-service.md)), over Kafka:

1. `orders/create` saves the order `Pending` and publishes `OrderCreatedEto`.
2. The product service sets the stock aside (and writes the stock history) and publishes `OrderStockReservedEto`, or `OrderStockRejectedEto`.
3. The shipment service makes the customer when there is none, takes the payment and publishes the internal `PaymentProcessedEto`; its handler sends the parcel and publishes `ShipmentCompletedEto`.
4. The order becomes `Confirmed`/`Delivered`, or `Cancelled` with the reason. The address of the order picks the outcome: any address is delivered, `nopay` is a declined payment, `fail` a parcel the carrier cannot reach.

The caller of a message (tenant, user, roles) travels in its envelope and a handler runs as that user ([ADR-0057](../docs/adr/0057-the-caller-of-a-message-and-publish-or-send.md)), so the audit fields of what a handler writes are the user's. Kafka has no queue per service, so a message
cannot be *sent* to one service there (`SendAsync` throws `NotSupportedException`); every Xyz event is published to its topic and each service reads it with a consumer group of its own. Events live in `Xyz.Shared/Contracts/Events/<publishing service>`, their handlers in the Host of the handling service
(`EventHandlers/<publishing service>`, `EventHandlers/Internal`) ([ADR-0059](../docs/adr/0059-where-events-and-their-handlers-live.md)).

## Run it

The hosts use the local infrastructure on its default ports: PostgreSQL `5432`, Redis `6379`, Kafka `9092`. They create their own databases and topics and wait for a dependency that is still starting (a broker that does not answer yet is tried again).

```bash
dotnet run scripts/generate-rsa-keys.cs -- samples/xyz/Xyz.AuthServer.Host/private_key.xml samples/xyz/Xyz.Shared/Hosting/public_key.xml   # once; both files are git-ignored, never reuse a key of another system
./samples/xyz/run-all.sh        # builds and starts the five hosts, Ctrl+C stops them
./samples/xyz/smoke-test.sh     # in another terminal: logs in, refreshes, runs the four order outcomes through the gateway
```

Demo accounts (development database only): `alice` / `alice123` (customer, tenant Acme) and `admin` / `admin123` (admin, system tenant). Seeded products: `LAMP-001` (stock 10) and `CHAIR-001` (stock 0).

```bash
TOKEN=$(curl -s -X POST localhost:5100/auth-server/v1/auth/login -H 'Content-Type: application/json' \
        -d '{"username":"alice","password":"alice123"}' | python3 -c 'import json,sys;print(json.load(sys.stdin)["payload"]["accessToken"])')
curl -s -X POST localhost:5100/product-service/v1/products/paged-list -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{}'
```

## Logging

Every host logs through `HsnSoft.Base.Logging.Serilog` (`PersistentLogger` section of its `appsettings.json`, see [docs/Logging.md](../docs/Logging.md)). The console gets everything from Debug up; Information and above go to **Graylog** when
`PersistentLogger:IsGrayLogActive` is `true` (GELF HTTP input `127.0.0.1:12202`, the local `infra-graylog`), otherwise to `Logs/<service>.txt`. Every log carries `Solution` (`Xyz`, `Abc`, ...: the first part of the assembly name), so in Graylog `Solution:Xyz` finds the whole Xyz system.

```bash
PersistentLogger__IsGrayLogActive=true ./samples/xyz/run-all.sh      # without the variable: log files
```

## Agt.Scheduler.Host: an agent

A second product ("Agt") with a different shape: no api, no authentication, no event bus. It takes **three** framework
packages, `AspNetCore` (only for the health endpoints), `Hosting` (the worker base) and `MongoDB`; no JWT, Swagger, broker or cache
package is loaded. Its output folder contains `HsnSoft.Base.AspNetCore/Core/Hosting/MongoDB` and the Mongo driver, nothing else.

The ASP.NET Core host gives it the lifetime, configuration, logging and the health endpoints a platform probes
(`/health/live`, `/health/ready` on port 5200). Every `Scheduler:PollIntervalSeconds` its worker (`RecentRecordsWorker`, built on
`PeriodicBackgroundService`) looks into the MongoDB collection `sample_records` and writes the ids of the records created in the
last `Scheduler:LookbackMinutes` minutes to its log. A failed run is logged and retried at the next tick.

```bash
dotnet run --project samples/agent/Agt.Scheduler.Host
```
```bash
# in another terminal: add a record (other applications write them, the agent only reads)
docker exec infra-mongo_container mongosh --quiet agt_scheduler \
    --eval 'db.sample_records.insertOne({name: "demo", createdAt: new Date()})'
```
```
info: Agt.Scheduler.Host.RecentRecordsWorker
      1 record(s) created in the last 5 minute(s): 6ada9c4caa3b2dd7783d88b3
```

The documents must carry a `createdAt` date; the field name is part of the contract with the writers.

## Def.Signer.Host: a monolith web api

A third product ("Def"): one host that answers fast and does the slow work later. Packages: `AspNetCore`, `AspNetCore.WebApi`,
`Caching.StackExchangeRedis`, `EventBus.Kafka`, `BlobStoring.Minio`, `EntityFrameworkCore.SqlServer`. There is no `Def.Shared`: one
host has nothing to share, so it composes the framework in its own `Program.cs`.

| Step | What happens |
|---|---|
| `POST /documents` | The `X-Api-Key` header is looked up in Redis (`def:apikey:{sha256 of the key}` -> owner; the keys are never stored). The request is published to Kafka (`def.SignRequested`) and answered with `202` and a request id. |
| Consumer | `SignRequestedHandler` runs on the Kafka consumer group `signer`, takes `Signer:ProcessingDelay` (2 s, standing in for slow work), builds a JSON document and saves it to MinIO (`signer-documents/{owner}/{requestId}.json`). |
| Metadata | The file's name, size and SHA-256 go to SQL Server (`documents`, unique on the request id, so a re-delivered message changes nothing). |
| `GET /documents/{id}` | `404` while queued, then the metadata. `GET /documents/{id}/content` streams the JSON from MinIO. A caller only sees its own documents. |

```bash
dotnet run --project samples/def/Def.Signer.Host      # http://localhost:5300, sample keys: acme-sample-api-key, globex-sample-api-key
curl -X POST localhost:5300/documents -H 'X-Api-Key: acme-sample-api-key' -H 'Content-Type: application/json' \
     -d '{"title":"Contract","content":"hello"}'
curl localhost:5300/documents/<requestId> -H 'X-Api-Key: acme-sample-api-key'
```

SQL Server needs about 2 GB of memory; a container limited to 1 GB restarts in a loop (error 17300 in its log).

## Abc: the identity service (in the making)

`samples/abc/service/identity` is the start of a new system, laid out as the production identity service is: `Domain` (`UserDomain/{Consts,Entities,Exceptions,Repositories}`),
`Application` (`Contracts/UserDomain/{Dtos,Services}` for the DTOs and the service interface, `Services/` for the implementation), `EntityFrameworkCore`
(`Context`, `Configurations`, `Repositories`) and `Http.Host` (`Controllers`). The host references Application and EntityFrameworkCore, both reference Domain.

The first use case is the CRUD of the users (SQL Server `Abc_Identity_Dev`, created at start; every use is written to a second context, `HistoryDbContext`, on PostgreSQL `Abc_History_Dev`, with the provider, the table prefix, the schema and the connection name in `EfCoreIdentityDbProperties` and `EfCoreHistoryDbProperties`, [ADR-0050](../docs/adr/0050-a-context-has-a-properties-class-for-its-database.md)); the controller only calls `IUserAppService`:

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

`samples/abc/gateway` has the two gateways of the Abc system, one per audience, built like the production gateway ([ADR-0027](../docs/adr/0027-gateways-per-audience.md)):
`Abc.Gateway.Commercial.Host` (port 5500, maps to the `commercial` segment of the services) and `Abc.Gateway.Public.Host` (port 5600, maps to `public`); what they share is `Abc.Shared.Hosting.Gateways` (on top of `Abc.Shared.Hosting`, which the identity service shares through `Abc.Shared.Hosting.Microservices`: [ADR-0028](../docs/adr/0028-shared-hosting-projects.md)).
Their routes are settings (`ReverseProxyRoutes`, `ServiceEndpoints`): a client calls `/identity-service/v1/...` and the gateway asks the service `/api/identity-service/v1/commercial/...` (or `public`).

```bash
# the identity service (5400), then a gateway; a token for the commercial one comes from Abc.AuthServer (5410)
curl -X POST http://localhost:5500/identity-service/v1/users/paged-list -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{}'   # commercial: needs the token (the service checks it)
curl -X POST http://localhost:5600/identity-service/v1/users/check-user-name -H 'Content-Type: application/json' -d '{"userName":"ada"}'            # public: open, a PublicUsersController endpoint
```

Each gateway reaches only the endpoints of its own segment (the other one answers 404), and its Swagger UI (`/swagger`) shows the identity service's document with the gateway's paths.

### Entities, DTOs and repositories in Abc

The entities derive from `FullAuditedEntity<Guid>` (and implement `IMultiTenant`), the lists use the framework's DTOs and the generic repository ([ADR-0042](../docs/adr/0042-ddd-domain-and-application-packages.md), [docs/DddBuildingBlocks.md](../docs/DddBuildingBlocks.md)). A list is asked for with
`pageNumber` (from 1), `maxResultCount` (10 by default, at most 100), `sortingText` (`"price desc, name"`: only the fields the service offers, `Validation:UnknownSortField` otherwise) and `searchText`, and answers `items`, `dataTotalCount`, `pageTotalCount`,
`resultDataStartNumber`, `resultDataEndNumber`.

### Localization in Abc

The answers are told in the language of the request: the `locale` claim of the token (a user has a `Language`: `admin` is `en`, `alice` is `tr`), else `Accept-Language`, else `en`; the languages are `en`, `tr`, `ru` ([ADR-0038](../docs/adr/0038-localization.md),
[docs/Localization.md](../docs/Localization.md)). The texts are JSON files in `Abc.Shared.Hosting/Localization` (what several services say) and in `Localization` of each service's domain project (its errors), merged with the framework's, the innermost first. Try it:
`POST /product-service/v1/products/get/{any id}` with alice's token answers in Turkish, with admin's in English (the `Accept-Language` header only counts without the claim); the login of an unknown user with `Accept-Language: ru` answers in Russian. The logs stay in English. The public endpoint `GET /identity-service/v1/users/welcome` (public gateway, no token) greets in the language of the header: `Accept-Language: tr` answers "Hoş geldiniz", `ru` "Добро пожаловать", anything else "Welcome".

### Tenants, audit and soft delete in Abc

Every Abc entity (`User`, `Product`, `Order`, `Shipment`) has the audit fields (`CreatedBy`, `UpdatedBy`: the id of the user), soft delete (a delete marks the row; the lists do not show it) and a tenant ([ADR-0037](../docs/adr/0037-audit-soft-delete-and-tenants.md),
[docs/EntityConcepts.md](../docs/EntityConcepts.md)). There are two tenants (`AbcTenants` in `Abc.Shared.Hosting`): the **system tenant** (`admin`: sees every tenant) and **Acme** (`alice`: sees only Acme's data, and the two seeded products are Acme's). The auth server puts
`tenant_id` (and `is_system_tenant` for `admin`) in the token. A row made by a user gets the tenant of the token; a user name and a sku are unique in the whole system. A database made before this change must be dropped (new columns): `drop database Abc_Identity_Dev`,
`Abc_Order_Dev`; the MongoDB ones (`Abc_Product_Dev`, `Abc_Shipment_Dev`) too, as their old documents lack the fields.

### The users of Abc

The auth server (which only reads) and the identity service (which owns the database) share one SQL Server database (`ConnectionStrings:EfCore-IdentityService`, `Abc_Identity_Dev`). The identity service creates it on its first start and, when it has no user, adds `admin` / `admin123` and `alice` / `alice123`
(passwords are kept as PBKDF2 hashes). `EnsureCreated` does not change a table that exists: a database made before this change has to be dropped (`drop database Abc_Identity_Dev`). The `CreatedBy` and `UpdatedBy` of a user are the
`sub` of the token of whoever made the request.

### The keys of the Abc tokens

The auth server signs its tokens with an RSA **private** key and every other Abc host checks them with the **public** key ([ADR-0031](../docs/adr/0031-jwt-signed-with-an-rsa-private-key.md)); the files are not in git. Make a pair once after a clone:

```bash
dotnet run scripts/generate-rsa-keys.cs -- samples/abc/app/Abc.AuthServer.Host/private_key.xml samples/abc/shared/Abc.Shared.Hosting/public_key.xml
```

The private key stays in `Abc.AuthServer.Host` (and is copied only to its output); the public key is copied next to every host that uses `Abc.Shared.Hosting`. A host whose key file is missing does not start and says which file. Never use the key of
another system here, and never commit a private key.

### The Abc apps

`samples/abc/app` has what people use: the auth server and the two web applications ([ADR-0029](../docs/adr/0029-one-auth-server-issues-the-token.md)).

| Project | Is | Port |
|---|---|---|
| `Abc.AuthServer.Host` | a web api: `POST /api/auth-server/v1/commercial/auth/login` (open) gives a JWT access token for a user name and password, `GET .../auth/me` says who the token is. The users are the identity service's, read from its database ([ADR-0032](../docs/adr/0032-the-auth-server-reads-the-users-of-the-identity-service.md)); the identity service makes the database and the users `admin` / `admin123` (admin, customer) and `alice` / `alice123` (customer) on its first start; start it before the first login | 5410 |
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

### The inbox in Abc ([ADR-0067](../docs/adr/0067-the-event-inbox-is-opt-in-and-a-decorator.md))

The identity service (SQL Server), the order service (PostgreSQL) and the product service (MongoDB) have an inbox (`AddEventInbox<...DbContext>()`, or `AddEventInbox("InboxMessage")` on MongoDB; the table or collection `InboxMessage` in their own databases): a message that comes twice is handled once there. The shipment service has none, and the Xyz system does not use the inbox at all.

### Redis and RabbitMQ in Abc

Every Abc microservice (and the auth server) and the commercial gateway are connected to **Redis** (`ConnectionStrings:Redis`, key prefix `abc:`); the four services (identity, order, product, shipment) are also connected to the **RabbitMQ** event bus (exchange `ABC`, the
client name of the service, `RabbitMq` section) ([ADR-0034](../docs/adr/0034-redis-and-rabbitmq-in-the-abc-hosts.md)). Both are checked by `/health/ready`, so a host without one is not ready (and `/health/live` stays 200). The infrastructure is the local one on its default ports:
Redis `6379`, RabbitMQ `5672` (`guest` / `guest`). Nothing publishes or consumes an event yet, and nothing reads the cache yet: the connections are what is there.

### The Abc services

`samples/abc/service` has the five-service shape of the system; each microservice is **four projects** ([ADR-0030](../docs/adr/0030-microservice-project-pattern.md)): `Abc.<X>Service.Domain`, `.Application`, a persistence project
(`.EntityFrameworkCore` or `.MongoDB`, the only thing that varies) and `.Http.Host`.

| Service | Persistence | Port | State |
|---|---|---|---|
| `identity` | `EntityFrameworkCore` (SQL Server `Abc_Identity_Dev`) | 5400 | users CRUD, a public endpoint |
| `order` | `EntityFrameworkCore` (PostgreSQL `Abc_Order_Dev`) | 5420 | orders with lines, CRUD |
| `product` | `MongoDB` (`Abc_Product_Dev`) | 5430 | products and stock, CRUD; two seeded products (`LAMP-001` with 10 in stock, `CHAIR-001` with 0) |
| `shipment` | `MongoDB` (`Abc_Shipment_Dev`) | 5440 | shipments of an order, CRUD |

Every service has the same endpoints as the users (`create`, `get/{id}`, `paged-list`, `filter-list`, `search-list`, `PUT`, `DELETE`) under `/api/{service}-service/v1/commercial/...`, and the commercial gateway forwards them as
`/order-service/v1/orders/...`, `/product-service/v1/products/...`, `/shipment-service/v1/shipments/...` ([ADR-0033](../docs/adr/0033-the-abc-services-crud-pattern.md)). A token is needed; `CreatedBy` / `UpdatedBy` are its `sub`. A database
made before this change must be dropped (`drop database Abc_Order_Dev`); the MongoDB services need nothing.

```bash
dotnet run --project samples/abc/service/order/Abc.OrderService.Http.Host         # http://localhost:5420
dotnet run --project samples/abc/service/product/Abc.ProductService.Http.Host     # http://localhost:5430
dotnet run --project samples/abc/service/shipment/Abc.ShipmentService.Http.Host   # http://localhost:5440
```

### The history of the users (a second context on PostgreSQL)

`HistoryDbContext` (PostgreSQL `Abc_History_Dev`, `ConnectionStrings:EfCore-HistoryService`) keeps what happened to the users: the identity service writes a record for each create, update and delete, and the auth server writes a `login` record for every token it
issues ([ADR-0052](../docs/adr/0052-the-history-of-the-users.md)). The records have a CRUD of their own, like the users': `POST /api/identity-service/v1/commercial/user-histories/{get/{id}, paged-list, filter-list, search-list, create}`, `PUT`, `DELETE /{id}`
(`IUserHistoryAppService`; the lists read only the columns of their DTO). Every context of a service has a properties class (`EfCoreIdentityDbProperties`, `EfCoreHistoryDbProperties`, `EfCoreOrderDbProperties`, `MongoProductDbProperties`,
`MongoShipmentDbProperties`) with the table (or collection) prefix, the schema, the provider and the connection name: no setting says which provider runs ([ADR-0051](../docs/adr/0051-every-context-is-named-by-its-properties-class.md)).
The connection strings are `EfCore-IdentityService` (SQL Server), `EfCore-HistoryService` (PostgreSQL), `EfCore-OrderService` (PostgreSQL), `Mongo-ProductService` and `Mongo-ShipmentService`.

### Environments ([ADR-0055](../docs/adr/0055-environments-of-the-abc-sample.md))

Every host has `appsettings.json` (what is the same everywhere) and `appsettings.development.json`, `.docker.json`, `.uat.json`, `.stage.json`, `.production.json` (what differs: urls, connection strings, RabbitMQ, Graylog, the services behind a gateway). The names are in small letters;
`development` has the values of a developer's machine and is what `dotnet run` and the IDE use (`Properties/launchSettings.json`); the others have fake values (`*.abc.example.com`, `CHANGE_ME`) to be overridden by environment variables or a secret store. The databases are `Abc_<Context>_<Suffix>`
with the suffix `Dev` (development and docker), `Uat`, `Stage` or `Prod`: `Abc_Identity_Dev` (SQL Server), `Abc_History_Dev`, `Abc_Order_Dev`, `Abc_ShipmentHistory_Dev` (PostgreSQL), `Abc_Product_Dev`, `Abc_Stock_Dev`, `Abc_Shipment_Dev` (MongoDB). The tables and the collections are named like their entity, in Pascal case and singular (`User`, `UserHistory`, `Order`, `OrderLine`, `Product`, `ProductStockHistory`, `Shipment`, `ShipmentHistory`, [ADR-0056](../docs/adr/0056-tables-and-collections-are-named-like-the-entity.md)); a database made before has to be dropped.

### The order flow ([ADR-0054](../docs/adr/0054-the-order-flow-through-events.md), where the events and the handlers live: [ADR-0059](../docs/adr/0059-where-events-and-their-handlers-live.md))

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
in stock, and there is a `delivered` record in the shipment history. An address with `fail` in it is returned by the (simulated) carrier and the order is cancelled, one with `nopay` has its payment declined and nothing is sent ([ADR-0060](../docs/adr/0060-customer-payment-and-an-internal-event-in-the-shipment-service.md); the shipment service has `customers` and `payments` with a CRUD each); an out of stock product (the Office Chair has 0) cancels it at the stock check.
The RabbitMQ management page (`localhost:15672`) shows the queues `ABC_<Service>_<Event>` (`ABC_ProductService_OrderCreated`). The events are in `Abc.Shared.Contracts/Events/<publishing service>/`, the handlers in the Host project of the service that handles them, `EventHandlers/<publishing service>/`. Who made the order (the tenant, the user, the roles) travels in the envelope of every message, and each handler runs as that user: the records the handlers write have `CreatedBy` and `UpdatedBy` ([ADR-0057](../docs/adr/0057-the-caller-of-a-message-and-publish-or-send.md)). The order has a `ShippingAddress` now: a database made before this change has to be dropped (`drop database Abc_Order_Dev`).

### More than one store in a service ([ADR-0053](../docs/adr/0053-more-than-one-store-in-a-service.md))

| Service | Entity (store) | History (store) | Written when |
|---|---|---|---|
| identity | `User` (EF Core, SQL Server `Abc_Identity_Dev`) | `UserHistory` (EF Core, PostgreSQL `Abc_History_Dev`) | a user is created, changed or deleted; the auth server issues a token (`login`) |
| order | `Order` (EF Core, PostgreSQL `Abc_Order_Dev`) | | |
| product | `Product` (MongoDB `Mongo-ProductService`, `Abc_Product_Dev`) | `ProductStockHistory` (MongoDB `Mongo-StockService`, `Abc_Stock_Dev`, registered under the key `stock`) | a product is created, or an update changes its stock |
| shipment | `Shipment` (MongoDB `Abc_Shipment_Dev`, project `Abc.ShipmentService.MongoDB`) | `ShipmentHistory` (EF Core, PostgreSQL `Abc_ShipmentHistory_Dev`, project `Abc.ShipmentService.EntityFrameworkCore`) | a shipment is delivered (`delivered`) or returned (`failed`) |

Each history has the CRUD of its entity: `POST .../user-histories`, `.../product-stock-histories`, `.../shipment-histories` with `get/{id}`, `paged-list`, `filter-list`, `search-list`, `create`, then `PUT` and `DELETE /{id}`. The connection strings of the new stores are
`Mongo-StockService` and `EfCore-ShipmentHistoryService`.
