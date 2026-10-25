# Xyz samples

A small event-driven microservice system built on the `HsnSoft.Base.*` framework projects. It is the proof that the
framework is product-independent: everything generic is in `src/`, everything about orders and shipments is here.

```
                 +-----------+      +-------------+
   client -----> |  Gateway  | ---> | Auth Server |   issues JWTs, refresh tokens in Redis
                 |  (YARP)   |      +-------------+
                 | JWT check |
                 | Redis rate|      +-----------+   OrderCreated    +-----------+
                 |  limit    | ---> |   Order   | ----------------> |  Product  |
                 +-----------+      | PostgreSQL|                   |  MongoDB  |
                       |            +-----------+ <-- StockRejected +-----------+
                       |                  ^        <-- StockReserved      |
                       |                  |                               | StockReserved
                       |                  | ShipmentDispatched            v
                       +--------------> (Product, Shipment)         +-----------+
                                                                    | Shipment  |
                                                                    |  MongoDB  |
                                                                    +-----------+
```

| Project | Port | Stores | Role |
|---|---|---|---|
| `Xyz.Gateway.Host` | 5100 | Redis | Single entry point (YARP). Validates the JWT, rate-limits per caller in Redis (`Xyz.Shared/RateLimiting`). |
| `Xyz.AuthServer.Host` | 5110 | Redis | `POST /api/auth/login`, `/refresh`, `/logout`, `GET /me`. Refresh tokens live in Redis. |
| `Xyz.ServiceOrder.Host` | 5120 | PostgreSQL, Redis, RabbitMQ | Places orders, follows their status. |
| `Xyz.ServiceProduct.Host` | 5130 | MongoDB, Redis, RabbitMQ | Products and stock; reserves stock for an order. |
| `Xyz.ServiceShipment.Host` | 5140 | MongoDB, Redis, RabbitMQ | Dispatches reserved orders. |
| `Xyz.Shared` | - | - | Product-specific: the composition of the framework, event contracts, roles, the "admin" policy. |

## The order flow

1. `POST /api/orders` saves the order as `Pending`, publishes `OrderCreated` and answers `202 Accepted`.
2. **Product** reserves the stock atomically and publishes `StockReserved` (or `StockRejected`).
3. **Shipment** reacts to `StockReserved`, dispatches and publishes `ShipmentDispatched`.
4. **Order** moves to `StockReserved`, `Rejected` or `Shipped` from those events.

Properties worth knowing:

- Handlers are **idempotent**: stock is reserved once per order id, a shipment exists once per order id, and an order
  only moves forward. RabbitMQ delivers at least once, so every message may arrive twice.
- A failing handler is retried 3 times, then the message goes to the dead-letter queue `xyz.events.dead-letter`.
- `Idempotency-Key` header on `POST /api/orders` (kept in Redis) makes a client retry safe.
- Not covered on purpose: the transactional outbox. The order is saved first and the event is published after it;
  a crash in between loses the event. It is the first thing the framework version should add.

## What is where

The infrastructure is **not** in the sample: it comes from the framework projects under `src/` (`HsnSoft.Base.*`), which
know nothing about Xyz. A second product ("Abc") would reference the same projects and write none of it again.

`Xyz.Shared` is the product's own decision about the framework: which capabilities every Xyz service uses (JWT-secured web
api, Redis cache, events) and in which order they are registered and run (`AddXyzApi`, `UseXyzApi`), plus the event
contracts, the role names and the "admin" policy. A host adds what is specific to it:

| Host | On top of Xyz.Shared |
|---|---|
| Gateway | - (its Redis rate limit is product code in `Xyz.Shared/RateLimiting`) |
| Auth server | - |
| Order | `EntityFrameworkCore.PostgreSql`, `EventBus.RabbitMQ` |
| Product, Shipment | `MongoDB`, `EventBus.RabbitMQ` |

Product-specific values are configuration: the key prefix (`Redis:InstanceName = "xyz:"`) and the exchange
(`RabbitMq:EventBus:ExchangeName = "xyz.events"`) are in each `appsettings.json`.

```csharp
builder.Services
    .AddXyzApi(builder.Configuration)                          // web api, Swagger, health, JWT, Redis
    .AddBasePostgres<OrderDbContext>(builder.Configuration)    // or .AddBaseMongo(builder.Configuration)
    .AddRabbitMqEventBus(builder.Configuration)
    .AddEventHandler<StockReserved, StockReservedHandler>();

app.UseXyzApi();   // exceptions, authentication, authorization, health, in that order
```

## File upload and download

`POST /api/products/{id}/image` (admin, `multipart/form-data`, a `file` part) and `GET /api/products/{id}/image` show how files are logged: never their
content, but the name and the size. The request log says the size the request declares (`RequestBytes`, the whole multipart body); the response log says
`..., upload urun-foto.jpg (2097152 bytes)` (the exact size of the file, known once the form is read) or `..., download urun-foto.jpg (2097152 bytes)`,
with `Files`, `FileCount`, `FileBytes` and `ResponseBytes`.

## What the logs say when something goes wrong

Errors the application expects (`KnownExceptions`: a rule that says no, a missing value, an unknown id, bad input, no token) are the
caller's: a service answers them with a 4xx and the response is logged as a **Warning**. Anything else (a null reference) is a bug of the
service: its global exception handler answers 500 and the response is an **Error**. The level of a response log is its status (2xx and 3xx
Information, 4xx Warning, 5xx Error), the same at the gateway and in a service, so an alert can count on it.

| Try | What happens | Level |
|---|---|---|
| a good order | `HttpRequest`, `HttpResponse` 202, then `ConsumeStart`/`ConsumeEnd` along the chain | Information |
| `quantity: 0` | the rule says no: 400 `BusinessRuleException` | Warning (`HttpResponse`) |
| `productId: "x"` | validation: 400 `validation_error` | Warning |
| no token | 401 | Warning |
| `productId` all zeros | the product service **rejects the message**: `CONSUME_EVENT_REJECTED`, no retry, not in the error queue | Warning (`ConsumeEnd`) |
| `quantity: 13` | a null reference in the order request: 500 | Error: the `HttpResponse` 500 of the service, the only log of the failure, with the cause and its stack (the level of a response is its status) |
| `quantity: 14` | a null reference in the handler of the product service: three attempts (`CONSUME_EVENT_RETRY`), `CONSUME_EVENT_ERROR` (the `ConsumeEnd`, with the stack), then the error queue (`RABBITMQ_CONSUMER_MOVED_TO_ERROR_QUEUE`) | Error (`ConsumeEnd`) |

`CardNumber` and `Phone` of the order request carry `[SensitiveData]` (`Xyz.Shared/Contracts`): they are hidden in the logged bodies. The
services write the request and response logs; the gateway writes one `HttpRequestResponse` only for what it answered itself (a rejected token,
an unknown route, a service that cannot be reached). The gateway of the sample does not log bodies; with `RequestLogging:LogBodies` on, it adds the
masked body of the request to a 5xx.

## Logging

Every host logs through `HsnSoft.Base.Logging.Serilog` (`PersistentLogger` section of its `appsettings.json`, see
[docs/Logging.md](../docs/Logging.md)). The console gets everything from Debug up; Information and above go to
**Graylog** when `PersistentLogger:IsGrayLogActive` is `true` (GELF HTTP input `127.0.0.1:12202`, the local `infra-graylog`),
otherwise to `Logs/<service>.txt` in the folder the host runs from. Debug never leaves the console. Every log carries
`Solution` (`Xyz`, `Agt`, `Def`: the first part of the assembly name), `Assembly` and `LoggerName`, so in Graylog
`Solution:Xyz` finds the whole Xyz system.

```bash
PersistentLogger__IsGrayLogActive=true ./samples/xyz/run-all.sh      # without the variable: log files
```

## Run it

The services use the local infrastructure on its default ports: PostgreSQL `5432` (`postgres`/`postgres`),
MongoDB `27017`, Redis `6379`, RabbitMQ `5672` (`guest`/`guest`). They create their own databases (`xyz_order`,
`xyz_product`, `xyz_shipment`), keys (`xyz:*`) and queues (`<service>.<Event>`), and wait for a dependency that is
still starting.

```bash
./samples/xyz/run-all.sh        # builds and starts the five hosts, Ctrl+C stops them
./samples/xyz/smoke-test.sh     # in another terminal: runs the whole flow through the gateway
```

Demo accounts: `alice` / `alice123` (customer) and `admin` / `admin123` (admin, can create products).
Seeded products: keyboard `11111111-1111-1111-1111-111111111111` (stock 100), mouse `22222222-2222-2222-2222-222222222222` (stock 3).

```bash
TOKEN=$(curl -s -X POST localhost:5100/api/auth/login -H 'Content-Type: application/json' \
        -d '{"username":"alice","password":"alice123"}' | python3 -c 'import json,sys;print(json.load(sys.stdin)["payload"]["accessToken"])')
curl -s -X POST localhost:5100/api/orders -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
     -d '{"productId":"11111111-1111-1111-1111-111111111111","quantity":2}'
```

The JWT signing key in the `appsettings.json` files is a sample value; a real system keeps it in a secret store.

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
# the identity service (5400), then a gateway; a token for the commercial one comes from Xyz.AuthServer (5110)
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

### Redis and RabbitMQ in Abc

Every Abc microservice (and the auth server) and the commercial gateway are connected to **Redis** (`ConnectionStrings:Redis`, key prefix `abc:`); the four services (identity, order, product, shipment) are also connected to the **RabbitMQ** event bus (exchange `abc.events`, the
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
with the suffix `Dev` (development and docker), `Uat`, `Stage` or `Prod`: `Abc_Identity_Dev` (SQL Server), `Abc_History_Dev`, `Abc_Order_Dev`, `Abc_ShipmentHistory_Dev` (PostgreSQL), `Abc_Product_Dev`, `Abc_Stock_Dev`, `Abc_Shipment_Dev` (MongoDB).

### The order flow ([ADR-0054](../docs/adr/0054-the-order-flow-through-events.md))

A signed-in customer (for example `alice`, of Acme, who gets her token from the auth server) makes an order; the services answer each other through RabbitMQ (exchange `abc.events`):

```
order    --OrderCreatedEto-->       product    stock of every line checked and set aside (stock history: "reserved")
product  --OrderStockCheckedEto-->  order      Pending -> Confirmed, or Cancelled with the reason
product  --ShipmentRequestedEto-->  shipment   only when the stock was set aside: the shipment is made and sent (shipment history: delivered / failed)
shipment --ShipmentCompletedEto-->  order      Confirmed -> Delivered, or Cancelled ("delivery failed")
```

`POST /api/order-service/v1/commercial/orders/create` with `{ "shippingAddress": "Ankara", "lines": [{ "productId": "...", "productName": "Desk Lamp", "unitPrice": 10, "quantity": 2 }] }`: a few seconds later the order is `Delivered`, the lamp has 2 less
in stock, and there is a `delivered` record in the shipment history. An address with `fail` in it is returned by the (simulated) carrier and the order is cancelled; an out of stock product (the Office Chair has 0) cancels it at the stock check.
The RabbitMQ management page (`localhost:15672`) shows the four queues `abc.events_<service>_<Event>`. The order has a `ShippingAddress` now: a database made before this change has to be dropped (`drop database Abc_Order_Dev`).

### More than one store in a service ([ADR-0053](../docs/adr/0053-more-than-one-store-in-a-service.md))

| Service | Entity (store) | History (store) | Written when |
|---|---|---|---|
| identity | `User` (EF Core, SQL Server `Abc_Identity_Dev`) | `UserHistory` (EF Core, PostgreSQL `Abc_History_Dev`) | a user is created, changed or deleted; the auth server issues a token (`login`) |
| order | `Order` (EF Core, PostgreSQL `Abc_Order_Dev`) | | |
| product | `Product` (MongoDB `Mongo-ProductService`, `Abc_Product_Dev`) | `ProductStockHistory` (MongoDB `Mongo-StockService`, `Abc_Stock_Dev`, registered under the key `stock`) | a product is created, or an update changes its stock |
| shipment | `Shipment` (MongoDB `Abc_Shipment_Dev`, project `Abc.ShipmentService.MongoDB`) | `ShipmentHistory` (EF Core, PostgreSQL `Abc_ShipmentHistory_Dev`, project `Abc.ShipmentService.EntityFrameworkCore`) | a shipment is delivered (`delivered`) or returned (`failed`) |

Each history has the CRUD of its entity: `POST .../user-histories`, `.../product-stock-histories`, `.../shipment-histories` with `get/{id}`, `paged-list`, `filter-list`, `search-list`, `create`, then `PUT` and `DELETE /{id}`. The connection strings of the new stores are
`Mongo-StockService` and `EfCore-ShipmentHistoryService`.
