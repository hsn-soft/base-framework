# Xyz samples

A small event-driven microservice system that uses only plain NuGet packages. It is the reference we will turn into
framework code: whatever the services repeat lives in `Xyz.Shared`, and that is what moves into the framework next.

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
| `Xyz.Gateway.Host` | 5100 | Redis | Single entry point (YARP). Validates the JWT, rate-limits per caller in Redis. |
| `Xyz.AuthServer.Host` | 5110 | Redis | `POST /api/auth/login`, `/refresh`, `/logout`, `GET /me`. Refresh tokens live in Redis. |
| `Xyz.ServiceOrder.Host` | 5120 | PostgreSQL, Redis, RabbitMQ | Places orders, follows their status. |
| `Xyz.ServiceProduct.Host` | 5130 | MongoDB, Redis, RabbitMQ | Products and stock; reserves stock for an order. |
| `Xyz.ServiceShipment.Host` | 5140 | MongoDB, Redis, RabbitMQ | Dispatches reserved orders. |
| `Xyz.Shared` | - | - | Contracts (events) and the **ServiceRegister** extensions every host uses. |

## The order flow

1. `POST /api/orders` saves the order as `Pending`, publishes `OrderCreated` and answers `202 Accepted`.
2. **Product** reserves the stock atomically and publishes `StockReserved` (or `StockRejected`).
3. **Shipment** reacts to `StockReserved`, dispatches and publishes `ShipmentDispatched`.
4. **Order** moves to `StockReserved`, `Rejected` or `Shipped` from those events.

Properties worth knowing:

- Handlers are **idempotent**: stock is reserved once per order id, a shipment exists once per order id, and an order
  only moves forward. RabbitMQ delivers at least once, so every message may arrive twice.
- A failing handler is retried 3 times, then the message goes to the dead-letter queue `xyz.dead-letter`.
- `Idempotency-Key` header on `POST /api/orders` (kept in Redis) makes a client retry safe.
- Not covered on purpose: the transactional outbox. The order is saved first and the event is published after it;
  a crash in between loses the event. It is the first thing the framework version should add.

## Xyz.Shared: what a host writes

```csharp
builder.Services
    .AddSharedWebApi("order-service")           // problem details, exception handler, OpenAPI, health checks
    .AddSharedJwtAuthentication(config)         // JWT bearer + "authenticated" / "admin" policies
    .AddSharedRedis(config)                     // IDistributedCache + IConnectionMultiplexer
    .AddSharedPostgres<OrderDbContext>(config)  // or .AddSharedMongo(config)
    .AddSharedRabbitMq(config)                  // event bus + background subscriber
    .AddEventHandler<StockReserved, StockReservedHandler>();

app.UseSharedPipeline();                        // exception handling, auth
app.MapSharedHealth();                          // /health/live, /health/ready
```

Each `AddShared...` is one file in `Xyz.Shared/Hosting/ServiceRegister.*.cs`.

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
        -d '{"username":"alice","password":"alice123"}' | python3 -c 'import json,sys;print(json.load(sys.stdin)["accessToken"])')
curl -s -X POST localhost:5100/api/orders -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
     -d '{"productId":"11111111-1111-1111-1111-111111111111","quantity":2}'
```

The JWT signing key in the `appsettings.json` files is a sample value; a real system keeps it in a secret store.
