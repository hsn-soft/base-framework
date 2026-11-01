# Health

What a platform (Kubernetes, a load balancer) asks a service, and what the service answers ([ADR-0117](adr/0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md)).

```csharp
builder.Services.AddBaseHealthChecks(builder.Configuration);   // the services; the packages add their checks to it
app.MapBaseHealth();                                           // /health/live, /health/startup, /health/ready
```

| Endpoint | Asks | A probe of the platform |
|---|---|---|
| `/health/live` | the process answers; no dependency is asked | `livenessProbe`: a failure restarts the pod |
| `/health/startup` | every check tagged `ready` | `startupProbe`: until it passes once, the other probes do not run (a slow start gets its time) |
| `/health/ready` | every check tagged `ready` | `readinessProbe`: a failure takes the pod out of the traffic |

A failed check answers `503`, and the body says which one:

```json
{"status":"Unhealthy","durationMs":12,"checks":[{"name":"redis","status":"Unhealthy","description":"Redis is not connected","durationMs":1},{"name":"rabbitmq","status":"Healthy","description":"RabbitMQ is connected","durationMs":0}]}
```

## The checks

A package adds its check when you add the package; the name is what the answer shows and what `Health:Disabled` takes.

| Package | Name | Healthy when |
|---|---|---|
| `Caching.StackExchangeRedis` | `redis` | the connection has a server and it answers a ping |
| `EventBus.RabbitMQ` | `rabbitmq` | the connection of the service to the broker is open (no connection of its own is opened by a probe) |
| `EventBus.Kafka` | `kafka` | the cluster answers a metadata request |
| `MongoDB` | `mongo` (`mongo-<key>` for a keyed database) | the database answers a ping |
| `EntityFrameworkCore.*` | `postgres`, `sqlserver`, `mysql` (`<provider>-<Context>` for a second context) | the context can connect |
| `BlobStoring.Minio` / `.Azure` / `.Aws` | `minio`, `azure-blob`, `aws-s3` | the store answers |

A check of the service itself (an API it cannot work without):

```csharp
builder.Services.AddBaseReadyCheck<BillingApiCheck>("billing-api");   // an IHealthCheck; it takes what it needs in its constructor
```

## Settings (section `Health`)

| Key | Default | |
|---|---|---|
| `CheckTimeout` | `00:00:05` | how long one check may take; the token of the check is cancelled when it passes. A check registered with a timeout of its own keeps it |
| `Disabled` | none | the names of the checks this service does not run: `"Disabled": [ "redis" ]` |
| `IncludeErrors` | `false` | whether a failed check also shows the message of its exception (it has addresses) |

## In Kubernetes

```yaml
startupProbe:   { httpGet: { path: /health/startup, port: 8080 }, periodSeconds: 3, failureThreshold: 40 }   # 2 minutes to start
readinessProbe: { httpGet: { path: /health/ready,   port: 8080 }, periodSeconds: 5, failureThreshold: 2 }
livenessProbe:  { httpGet: { path: /health/live,    port: 8080 }, periodSeconds: 10, failureThreshold: 3 }
```

The readiness probe does not wait for a dependency to come back for ever: a pod that cannot reach one is taken out of the traffic and put back when it can.
