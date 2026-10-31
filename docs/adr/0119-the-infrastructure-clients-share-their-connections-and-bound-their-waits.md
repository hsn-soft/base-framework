# ADR-0119: The infrastructure clients share their connections, bound their waits and retry only what is safe to retry

- **Status:** Accepted
- **Date:** 2026-10-31 (the findings of the review of the framework for a service that runs as many pods under load; adds to [ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md))

## Context

A review of the infrastructure packages for what happens in production (many pods, a dependency that is slow or goes away, a long run) found places where a service holds more than it needs or waits for ever. Each is a small decision; they are kept together here, with the reason of each, and the list grows as the review goes on.

## Decision

- **Redis: the cache and the service share one connection.** `AddBaseRedis` registered the cache (`IDistributedCache`), which made a multiplexer of its own from the connection string, and an `IConnectionMultiplexer` for the service: two multiplexers, which is four connections to Redis in every pod, and two states of
  the connection (the readiness check could say healthy while the cache's own connection was down). The cache is now given the connection of the service (`RedisCacheOptions.ConnectionMultiplexerFactory`): two connections per pod, one state, and the readiness check looks at the connection the cache uses.
  The cache closes the connection when it is disposed, which the container does at the end with the rest of the singletons (tried: wrapping the connection so that it cannot be closed does not work, the cache reaches it through its database); nothing uses Redis after that.

- **MongoDB: the waits are bounded** (section `Mongo`, `MongoOptions`): a server is waited for 10 seconds (the driver's 30), opening a connection may take 10 seconds, and a read or a write on a connection waits at most 2 minutes. The driver waits for ever on a connection that has stopped answering (what a network failure
  leaves half open), so a request of a pod hung until the system closed the socket. An option that the connection string names (`serverSelectionTimeoutMS`, `connectTimeoutMS`, `socketTimeoutMS`) is the connection string's. The socket timeout is the longest the server may be silent, not the length of a request, so a long
  result that keeps arriving is not cut.
- **S3 and MinIO: a total time for a request is opt-in** (`AwsS3:TimeoutSeconds`, `AwsS3:MaxErrorRetry`, `Minio:TimeoutSeconds`), not a default. The AWS client has no total limit by default (a call to a server that does not answer waits for ever), but it was **measured** that the limit, once set, covers
  the whole request: a stored file of 160 MB was cut at a limit of 2 seconds. A default would cut the large uploads and the slow downloads of a service, which only the service knows how long they need. Set it to what the largest file needs at the slowest connection plus some. Azure's client already has
  its own bounds (a network timeout and three retries) and is left as it is.

- **Entity Framework: the retries and the command timeout are bounded and set by the section `EntityFramework`** (`EfCoreOptions`, passed to the three providers): three retries with at most 5 seconds between them (EF's default is six, up to 30 seconds, about a minute of waiting for a request on a database that is down), a command is ended after
  30 seconds, and `MaxRetryCount: 0` turns the retries off. **A transaction started by the code is refused by a retrying strategy** (it was measured: the first command in it throws), so `ExecuteInTransactionAsync` runs a unit of work in a transaction through the strategy: the whole unit is run again from its
  start when its commands fail in a way that may pass, which is why the work must be safe to run again (it reads what it needs inside it).

- **The `Jwt` section is checked when the host starts** (`AddBaseJwtAuthentication`): a blank issuer or audience (no token would ever be accepted, and nothing says why), an HMAC key shorter than 32 bytes (the length was only written in a comment; a short key worked, a blank one failed at the first request), an RSA key of
  fewer than 2048 bits, a key file that is not there, or `AccessTokenMinutes` under 1 stops the host, with all the reasons in one message. The length of an HMAC key is counted in bytes.

- **The Swagger gateway keeps one document for each service, not one for each address.** The cache key held the address of the gateway, which is the `Host` header of the request when `SwaggerGateway:PublicOrigin` is not set: a client that sends a new `Host` every time made a new entry (a whole document)
  every time. What is kept is the document the service gave, under the key of the service; the address and the paths are put in for each request.

- **Serilog: the sink that was replaced a moment ago is flushed when the application stops.** The Graylog sink is rebuilt every few minutes (a broken one would otherwise stay broken), and the old one lives on for ten seconds so what it is still sending is not lost; but the timer that disposes it does not live
  to fire when the application ends in those ten seconds, and the sink sends its batch only when it is disposed, so that batch was lost. Disposing the sink now disposes the ones that are waiting too, and a refresh after that builds nothing.

- **NLog: the connection to Graylog is made again every two minutes and a Graylog that is not there is found in three seconds; the queue is bounded on purpose.** The target made its `HttpClient` with the defaults, which keep a connection for ever (a Graylog that moved was written to at the old address until the
  connection broke) and wait ten seconds for a connection to a Graylog that is down, which every log waits for before the next. The queue of logs that wait is 10 000 and drops the logs that do not fit (it did so already, by default, without saying it): a Graylog that is down for an hour costs logs, never the memory of the pods.

- **Localization: the missing keys that are remembered are bounded** (1 000). A missing key is logged once, which needs a memory of the keys already logged; a key made of what a client sent (an error code taken from a request) made an entry for every value, for as long as the process lives. After
  1 000 the later ones are neither remembered nor logged, and that is said once.

## Consequences

- The connections to Redis of a system are halved (the test counts them on a real Redis: two for a host).
- A host with a weak or incomplete `Jwt` section does not start any more: the settings of every host of the samples were already right.
- A request to a database that is down fails after about ten seconds of retries instead of about a minute; a service that needs a transaction calls `ExecuteInTransactionAsync` and not `BeginTransactionAsync`.
- A request to MongoDB that waits on a dead connection ends in at most two minutes (the connection is dropped and the driver tries again), and one that finds no server in ten seconds.
- A service that makes its own use of Redis after the cache is disposed would find the connection closed: at the end of the process only.

## Related

[ADR-0117](0117-the-health-endpoints-are-live-startup-and-ready-and-answer-with-the-checks.md).
