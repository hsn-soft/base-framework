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
    .AddXyzApi(builder.Configuration)                          // web api, OpenAPI, health, JWT, Redis
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
| `quantity: 13` | a null reference in the order request: 500 | Error: `GLOBAL_EXCEPTION_HANDLER` (the cause, with the stack) and the `HttpResponse` 500 of the service: the level of a response is its status |
| `quantity: 14` | a null reference in the handler of the product service: three attempts (`CONSUME_EVENT_RETRY`), `CONSUME_EVENT_ERROR`, then the error queue | Error (`ConsumeEnd`) |

`CardNumber` and `Phone` of the order request carry `[SensitiveData]` (`Xyz.Shared/Contracts`): they are hidden in the logged bodies. The
services write the request and response logs; the gateway writes one `HttpRequestResponse` only for what it answered itself (a rejected token,
an unknown route, a service that cannot be reached). The gateway of the sample does not log bodies; with `RequestLogging:LogBodies` on, it adds the
masked body of the request to a 5xx.

## Logging

Every host logs through `HsnSoft.Base.Logging.Serilog` (`FrameworkLogger` section of its `appsettings.json`, see
[docs/Logging.md](../docs/Logging.md)). The console gets everything from Debug up; Information and above go to
**Graylog** when `FrameworkLogger:IsGrayLogActive` is `true` (GELF HTTP input `127.0.0.1:12202`, the local `infra-graylog`),
otherwise to `Logs/<service>.txt` in the folder the host runs from. Debug never leaves the console. Every log carries
`Solution` (`Xyz`, `Agt`, `Def`: the first part of the assembly name), `Assembly` and `LoggerName`, so in Graylog
`Solution:Xyz` finds the whole Xyz system.

```bash
FrameworkLogger__IsGrayLogActive=true ./samples/run-all.sh      # without the variable: log files
```

## Run it

The services use the local infrastructure on its default ports: PostgreSQL `5432` (`postgres`/`postgres`),
MongoDB `27017`, Redis `6379`, RabbitMQ `5672` (`guest`/`guest`). They create their own databases (`xyz_order`,
`xyz_product`, `xyz_shipment`), keys (`xyz:*`) and queues (`<service>.<Event>`), and wait for a dependency that is
still starting.

```bash
./samples/run-all.sh        # builds and starts the five hosts, Ctrl+C stops them
./samples/smoke-test.sh     # in another terminal: runs the whole flow through the gateway
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
packages, `AspNetCore` (only for the health endpoints), `Hosting` (the worker base) and `MongoDB`; no JWT, OpenAPI, broker or cache
package is loaded. Its output folder contains `HsnSoft.Base.AspNetCore/Core/Hosting/MongoDB` and the Mongo driver, nothing else.

The ASP.NET Core host gives it the lifetime, configuration, logging and the health endpoints a platform probes
(`/health/live`, `/health/ready` on port 5200). Every `Scheduler:PollIntervalSeconds` its worker (`RecentRecordsWorker`, built on
`PeriodicBackgroundService`) looks into the MongoDB collection `sample_records` and writes the ids of the records created in the
last `Scheduler:LookbackMinutes` minutes to its log. A failed run is logged and retried at the next tick.

```bash
dotnet run --project samples/Agt.Scheduler.Host
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
dotnet run --project samples/Def.Signer.Host      # http://localhost:5300, sample keys: acme-sample-api-key, globex-sample-api-key
curl -X POST localhost:5300/documents -H 'X-Api-Key: acme-sample-api-key' -H 'Content-Type: application/json' \
     -d '{"title":"Contract","content":"hello"}'
curl localhost:5300/documents/<requestId> -H 'X-Api-Key: acme-sample-api-key'
```

SQL Server needs about 2 GB of memory; a container limited to 1 GB restarts in a loop (error 17300 in its log).
