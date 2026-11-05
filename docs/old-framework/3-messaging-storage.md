# Cluster C: messaging, storage, cache (old framework on `dev-backup` vs new tree)

Method: read every `.cs` of the old projects with `git show dev-backup:<path>`, read the new `src/` projects, `docs/EventBus.md`, ADRs 0003, 0023, 0057, 0062-0067, 0076, 0098-0104, 0112, 0119, 0127, and grepped the hhs system (`/Users/hasansahin/ws/org/tech-summus/hhs`) for real usage. Old tests: `dev-backup:test/` has no test of any project in this cluster (only `HsnSoft.Base.Test.Api*` sample hosts and `Test.Unit`; one `ForgotPasswordMailSendEtoHandler` sample).

## Summary

| Old project | Now | Verdict | Gap / priority |
|---|---|---|---|
| `EventBus` | same name, rewritten (`src/HsnSoft.Base.EventBus`) | sound idea, over-coupled and static-ish; superseded | Outbox: **Medium** (hhs code itself names it a known gap). Rest: covered |
| `EventBus.RabbitMQ` | same name, rewritten | sound core, flawed details; superseded | covered; nothing to port except 2 ideas |
| `EventBus.Kafka` | same name, rewritten (ADR-0062..0066, 0104, 0127) | flawed (at-most-once, client per publish); superseded | covered |
| `EventBus.Azure` | **missing** (not ported; open item in ADR-0023/0025/INDEX) | half-working prototype; flawed | Azure Service Bus: **Low** (no hhs/Hsx use), keep as open item |
| `BlobStoring` (+ `.Aws`, `.Azure`, `.Minio`) | same names, written from scratch | old = **csproj-only skeletons, zero C#** | covered |
| `BlobStoring.FileSystem` | **missing**, never had code | skeleton | **Low** (add only if a local-disk store is wanted) |
| `Caching.StackExchangeRedis` | same name, much smaller (`AddBaseRedis`, options, health check) | sound for the cache part; the rest flawed or product-only | lock/repository/limit-store: keep dropped; DataProtection-on-Redis: **Low**, document only |

Two cross-cutting findings: (1) the old packages never registered themselves in DI; hhs `Hhs.Shared.Hosting/Extensions/ServiceCollectionExtensions.cs` wired `RabbitMqPersistentConnection`, `EventDispatcher`, `FailedEventNotifier`, the subscription manager and `EventBusRabbitMq` by hand. The new `AddRabbitMqEventBus` removes that. (2) the old `EventBus` references `Serilog`, `Json`, `MultiTenancy` (forced, see below); the new one references only Core plus three `Microsoft.Extensions.*` abstractions.

---

## 1. `HsnSoft.Base.EventBus` (old)

### What it was
- Files (`dev-backup:src/HsnSoft.Base.EventBus/HsnSoft/Base/...`): `Domain/Entities/Events/{IIntegrationEventMessage, MessageEnvelope<T>, ParentMessageEnvelope, FailedEto, ReQueuedEto}`, `EventBus/{IEventBus, IIntegrationEventHandler<T>, IEventBusSubscriptionManager, IEventApplicationService, EventBusConfig, IntegrationEventInfo, IntegrationEventHandlerInfo}`, `Attributes/{EventNameAttribute, GenericEventNameAttribute, IEventNameProvider}`, `SubManagers/InMemoryEventBusSubscriptionManager`, `Logging/{IEventBusLogger, EventBusLogger, EventBusLogModels, EventBusLogFacility}`, `Extensions/GenericTypeExtensions`.
- References: `HsnSoft.Base.Serilog`, `HsnSoft.Base.Json`, `HsnSoft.Base.MultiTenancy` (project refs). No Core reference stated; it got Core transitively.
- Envelope (flat JSON): `HopLevel`, `ReQueuedCount`, `MessageId` (Guid v7), `MessageTime`, `CorrelationId`, `UserId` (string), `UserRoles` (comma-joined string), `ClientLat/Long/Channel/Version`, `Producer` (= `ExchangeName_ConsumerClientName`), plus `ParentMessageId`, `Message`.
- Naming: event name = `type.Name` minus `EventNamePrefix` and `EventNameSuffix` (default suffix `Eto`) - done by `EventNameGetter` (a settable `Func<string,string>` on the subscription manager, set by the bus in its constructor). `EventNameAttribute`/`GenericEventNameAttribute` exist but **nothing reads them** (the sub manager and all buses use `eventType.Name`): dead code.
- Publish API: `PublishAsync(msg, parentMessage, correlationId, isExchangeEvent=true, isReQueuePublish=false)`; `Subscribe<TEvent,THandler>(fetchCount=1)` called at startup by hand (`UseEventBus`).
- Failure model: `FailedEto` (reason, time, `dynamic` object, type name) and `ReQueuedEto` (consumer, object, type name). Error queue per `ErrorClientName`.
- Logging: persisted `ProduceMessageLogModel`/`ConsumeMessageLogModel` through a Serilog `PersistentLogger` subclass (static-ish Serilog dependency inside the contract package).
- Motto fit: **poor** on coupling. A package that only holds the contract forced Serilog, a JSON package and MultiTenancy on every consumer. `InMemoryEventBusSubscriptionManager` is a non-thread-safe `Dictionary` mutated at runtime; `GetHandlersForEvent`/`GetEventInfoByName` use indexers (throw `KeyNotFoundException`); `AddSubscription` throws bare `TypeAccessException`; `Clear()` clears handlers but not `_eventList`. `FailedEto.FailedMessageObject` is `dynamic` (record with a `dynamic` primary-constructor parameter plus an `object` property: confusing).

### Where it is now
(a) Same name, rewritten: `src/HsnSoft.Base.EventBus` (refs Core + 3 `Microsoft.Extensions.*` abstractions). Mapping:
- Envelope/ETOs: `Domain/Entities/Events/*` kept in the same namespace (`MessageEnvelope<T>`, `ParentMessageEnvelope`, `FailedEto`, `ReQueuedEto` with `object?`).
- Naming: `EventNames.For/Trim(EventBusOptions, Type)` (static, takes options; no mutable getter).
- Subscription manager: `IEventBusSubscriptionManager`/`InMemoryEventBusSubscriptionManager` + `EventSubscription` (registered through the container, `AddEventHandler/AddEventHandlers`).
- Dispatcher, serializer, retry: `EventDispatcher`, `EventSerializer` (System.Text.Json only), `PublishRetry`, `EventEnvelopeFactory`, `EventContext` (ambient parent), `IEventHandlerScope`, `IEventHeaderProvider`, `EventBusBase`, `EventBusOptions`.
- New: `Inbox/*` (contract only: `IInboxStore`, `InboxMessage`, options, `MessageInProgressException`), `SendAsync` (command to one service), `CancellationToken` everywhere.
- Dropped: `EventNameAttribute`, `GenericEventNameAttribute`, `IEventNameProvider`, `GenericTypeExtensions` (c: intentionally dropped), `IEventBusLogger` and the persisted log models (replaced by `ILogger` events `PRODUCE_EVENT_*`/`CONSUME_EVENT_*`, `docs/EventBus.md` "Moving from the previous framework").
- Wire changes vs old (verify before a mixed rollout, the maintainer says compatibility is not required): `UserRoles` is now `List<string>` (old: comma-joined string; a new reader of an old message with a string would throw `JsonException`, inferred from the two types, not tested here); `Producer` is `ConsumerClientName` (old: `Exchange_Client`); `UserId` is `Guid?`; `ClientLat/Long/Channel/Version` are header keys in the extension-data dictionary (same JSON names, so those stay compatible); `TenantId`/`IsSystemTenant` are new headers.

### Why
ADR-0023 (port of production code, same wire), ADR-0057 (caller in the envelope, publish vs send, inbox/outbox optional, `ActingUser`), ADR-0003 (abstractions apart from providers), ADR-0018/0020 (logs, referenced from ADR-0023). Dropping the attributes: inferred (nothing used them; hhs has no `[EventName]` usage either: grep of hhs found none).

### Verdict
Superseded by something better. The new core removes the forced Serilog/Json/MultiTenancy coupling, the mutable `EventNameGetter`, reflection-by-`dynamic` in the logging path, and the hand-written startup `Subscribe` calls; it adds scoped handler execution as the caller, inbox decorator, bounded concurrency.

### Gap
- **Outbox: Medium.** hhs `service/content/.../ApplicationServiceBase.cs:22-25` says in a comment: "KNOWN GAP: EventBus.PublishAsync is called after DB writes with no transactional guarantee. If a publish fails after a successful DB commit the event is permanently lost. Mitigation: implement an Outbox table + relay worker in each service." `samples/hsx/docs/hhs-analysis.md` says "Outbox: Not needed: hhs has none either". These two statements disagree: hhs has none, but its own code calls it a risk. ADR-0057 leaves room for it ("an outbox that publishes with the change that caused the message"). Recommendation: **add a package** `HsnSoft.Base.EventBus.Outbox`-style pair only when Hsx wants it: contract in `EventBus` (an `IEventBus` decorator that writes to an `IOutboxStore` instead of the broker) plus `EventBus.EntityFrameworkCore`/`EventBus.MongoDB` providers (same split as the inbox, ADR-0067), a relay `BackgroundService`. It may reference only EventBus, and the EF/Mongo package already there. Users: Hsx services that write a row and publish (content, text-normalizer, video-generator chains).
- Everything else of this project: **already covered** (see the table above).

### Keep & avoid
- Keep (already kept): flat envelope, `HopLevel`/`ParentMessageId`/`ReQueuedCount`/`CorrelationId` chain, Guid v7 ids, `FailedEto`/`ReQueuedEto` loop, `EventNamePrefix/Suffix`.
- Avoid: attributes that nothing reads; a contract package that references logging/serialization/tenancy; a mutable name getter set by whichever bus is constructed last; `dynamic` in logging paths (`((dynamic)messageEnvelope)?.X` x12 in the old dispatcher, slow and unsafe); indexers that throw on a missing event.

---

## 2. `HsnSoft.Base.EventBus.RabbitMQ` (old)

### What it was
- Files (`dev-backup:src/HsnSoft.Base.EventBus.RabbitMQ/HsnSoft/Base/EventBus/RabbitMQ/`): `EventBusRabbitMq`, `RabbitMqConsumer`, `EventDispatcher`, `FailedEventNotifier`, `EventNameHelper`, `RabbitMqHealthCheck`, `Connection/RabbitMqPersistentConnection`, `Configs/{RabbitMqConnectionSettings, RabbitMqEventBusConfig, DeliveryMode}`.
- References: `RabbitMQ.Client` 7.2.1, `Polly` 8.7.0, `Microsoft.Extensions.Diagnostics.HealthChecks`, project `EventBus` (which brings Serilog, Json, MultiTenancy). No DI extension in the package (hhs wired it).
- Topology (same as new docs): direct exchange `{ExchangeName}` (declared non-durable by default), queue `{Exchange}_{Consumer}_{Event}` durable, bound with the event name; error queue `{Exchange}_{ErrorClient}_{Event}`; requeue queue `{ConsumerInfo}_{Event}`; persistent + `mandatory` messages; manual ack; QoS prefetch = `FetchCount`.
- Retry: publish `Polly` `WaitAndRetryAsync(5, 2^n s)` on `BrokerUnreachableException`/`SocketException`; consumer: `TimeoutException` => wait 5 s then `BasicNack(requeue:true)`; any other exception => publish `FailedEto` to error queue, then ack the original; a failing `FailedEto` is logged and dropped (loop guard); if the notify itself fails => requeue.
- Dispatcher (`EventDispatcher`): one DI scope per handler, resolves handler by type, disables `IMultiTenant` and `IScopeSubscription` data filters, invokes `HandleAsync` by reflection (`GetMethod(...).Invoke`), logs a persisted success/error model, rethrows.
- Connection: `AutomaticRecoveryEnabled`, `TopologyRecoveryEnabled`, 10 s recovery interval, 60 s heartbeat; `TryConnectAsync` under a `lock` doing sync-over-async (`.GetAwaiter().GetResult()` inside `lock`); `GetRabbitMqConnectionCountAsync` calls `http://host:{AMQP port}/api/connections` with basic auth (wrong port: the management API is 15672) and creates a `new HttpClient()` per connect.
- Consumer: one `RabbitMqConsumer` per subscribed event with its own channel, a 5 s recovery worker for a dead channel, a `SemaphoreSlim(fetchCount)`, `Dispose` waits up to 30 s with `Thread.Sleep`.
- Publisher: **a new channel per publish** (`await using var publisherChannel = ...CreateModelAsync()`), no publisher confirms, `_isPublishing` is a single int flag (set to 0 by whichever publish ends first, so concurrent publishes miscount; also never reset when the publish throws).
- Motto fit: reasonable function, poor discipline: forced Serilog/tenancy via EventBus, sync-over-async in `Subscribe` (blocks the host's startup thread), `Task.Run` fire-and-forget recovery loop, reflection dispatch, channel per publish, an HTTP client per connect, the tenant-filter hard-coding inside the dispatcher.

### Where it is now
(a) Same name, rewritten: `src/HsnSoft.Base.EventBus.RabbitMQ` (refs `EventBus`, `RabbitMQ.Client` 7.2.2, HealthChecks, Hosting.Abstractions, Options.ConfigurationExtensions; no Polly, no Serilog). Types: `EventBusRabbitMq`, `RabbitMqPublisher` (confirms, one `IsTransient` list), `PublisherChannelPool`, `RabbitMqSubscriber` (hosted service), `RabbitMqConsumer`, `FailedEventNotifier`, `EventNameHelper`, `RabbitMqHealthCheck`, `Connection/{RabbitMqConnectionFactory, RabbitMqPersistentConnection, RabbitMqNotConnectedException}`, `RabbitMqClientLogBridge/Service`, `AddRabbitMqEventBus`. The tenant filter disabling is gone from the dispatcher: the tenant travels as headers and `MultiTenancy.EventBus` (`TenantEventHandlerScope`) enters the message tenant (ADR-0074).
- Same names/behaviour kept: topology table, `RequeueDelay`, FailedEto loop guard, section names `RabbitMq:Connection` and `RabbitMq:EventBus`.

### Why
ADR-0023 (port, bug fixes), ADR-0099 (confirms + timeout, one transient list), ADR-0103 (TLS, default-credentials warning), ADR-0127 (no channel per message, publisher pool), ADR-0098 (in-progress message put back), ADR-0057. The removal of the management-API call is stated in `docs/EventBus.md`.

### Verdict
Old: sound topology and failure loop, flawed implementation (no confirms means a published message could be lost silently; channel per publish; sync-over-async; miscounted in-flight publishes; wrong-port HTTP call). Superseded by the new package.

### Gap
None. **Already covered.** Check one thing only: **dead letters**. Neither old nor new uses a RabbitMQ DLX/dead-letter queue; the application-level `FailedEto` to the error client is the dead-letter mechanism (and that is what hhs `event-manager` consumes). Not a gap while Hsx keeps an event-manager service (the hhs-analysis says that one is a product service to build).

### Keep & avoid
- Keep (kept): the `FailedEto` + `ReQueuedEto` + error-queue loop; one consumer per event with its own channel and a recovery probe; `ConsumerDispatchConcurrency = FetchCount` (the old comment: without it the client serializes deliveries despite prefetch); parallel `Dispose` of consumers (bounded by the slowest, `EventBusRabbitMq.Dispose` comment); logging-only handlers for `AutomaticRecovery` events (do not call `TryConnect` mid-recovery).
- Avoid: publishing without confirms; a channel per message; a flag that only means "one is publishing"; HTTP calls to the management API on the AMQP port; `lock` around `.GetAwaiter().GetResult()`; `new HttpClient()` per call; business-tenant logic inside a broker package.

---

## 3. `HsnSoft.Base.EventBus.Kafka` (old)

### What it was
- Files: `EventBusKafka`, `KafkaConsumer`, `KafkaProducer`, `Configs/{KafkaConnectionSettings, KafkaEventBusConfig}`, `Converters/{CustomeTimeSpanConverter, DefaultJsonOptions, SerializePropertyAsDefaultConverter}`. References: `Confluent.Kafka` 2.15.0, project `EventBus`. **Uses Newtonsoft** (`JsonConvert`) while the RabbitMQ bus uses System.Text.Json: two wire formats for the same envelope (Newtonsoft default writes `DateTime` and `TimeSpan` differently, hence the custom `TimeSpan` converter).
- Behaviour read in code:
  - Producer: a **new `KafkaProducer` and a new Confluent producer per `PublishAsync`**, `Flush(10s)`, then `Thread.Sleep(50)`; `Acks.All`, idempotence, 3 retries, `Debug="msg"`; key = `DateTime.UtcNow.Ticks` (long): useless for partitioning and ordering by entity.
  - Consumer: one consumer per `Subscribe`, group id = `ConsumerClientInfo` (so **all events of a service share one group** and one `Subscribe` creates a second consumer in the same group on a different topic), `EnableAutoCommit=false`, **`consumer.Commit(result)` happens before the message is handled** (at-most-once: a crash loses the message), then `OnMessageReceived` fires a `Task.Run`; `Thread.Sleep(200)` per loop; `AutoOffsetReset=Earliest`.
  - Handler: `_serviceProvider.GetService(handlerType)` from the **root** provider (scoped handlers resolve wrongly or throw), a handler that is missing is silently skipped (`continue`), a handler exception is logged and swallowed: **no `FailedEto`, no dead letter, no retry**, and `isExchangeEvent`/`SendAsync` semantics ignored (`isReQueuePublish` only counts).
  - `_messageProcessorTasks` is a plain `List<Task>` mutated from many threads (race); `Dispose` blocks with `Task.WaitAll`.
  - `fetchCount` is dropped (`AddSubscription(eventType, handlerType)` without it).
  - Fatal-error handler sets `KeepConsuming=false` forever: a consumer that hits a fatal error stops and never restarts.
- Motto fit: poor (client per message, thread-pool `Task.Run` per message, Newtonsoft, forced coupling through EventBus).

### Where it is now
(a) Same name, rewritten: `src/HsnSoft.Base.EventBus.Kafka` (refs `EventBus`, `Confluent.Kafka` 2.16.0, HealthChecks, Hosting.Abstractions). `KafkaEventBus` (one long-lived producer, `Acks.All`, idempotence, key = correlation id or message id), `KafkaSubscriber`, `KafkaEventConsumer` (one thread per event, group `{Exchange}_{Consumer}_{Event}`, `fetchCount` parallel with `OffsetTracker` commits only after earlier offsets finish), `KafkaTopology` (topic `{Exchange}_{Event}`, `{Exchange}_dead-letter`), `KafkaRecovery` (back-off), `KafkaHealthCheck` (one admin client), `KafkaOptions` (TLS/SASL, timeouts, `CreateTopics`).

### Why
ADR-0023 (note on Kafka), ADR-0062 (consumer and group per service and event), 0063 (topic names), 0064/0065 (consumers outlive failures; not read in detail), 0066 (fetchCount), 0104 (back-off, timeouts), 0103 (TLS/SASL), 0127 (thread, admin client), 0067/0098 (inbox, in-progress retry). `docs/adr/INDEX.md` still lists "Port the Kafka consumer..." under open items although it is done: stale line.

### Verdict
Flawed (loses messages by committing first, no failure path, one group per service, client per publish). Superseded; the new one also fixes the Newtonsoft/System.Text.Json wire split (one `EventSerializer`).

### Gap
None for hhs (hhs never used Kafka; grep of hhs for `kafka` in code and config found nothing). Kafka is covered for Hsx if chosen. **Already covered.** Note a design difference to keep in mind: Kafka has no `SendAsync` (refused, ADR-0057), so a requeue (`ReQueuedEto`) cannot work on Kafka; the manual-requeue flow of the event manager is RabbitMQ-only. State that in the Hsx event-manager design.

### Keep & avoid
- Keep: `MaxPartitionFetchBytes`/`FetchMaxBytes` large-message settings (the old defaults were 50 MB: worth checking whether `KafkaOptions` exposes them; **not verified**), `Acks.All` + idempotence.
- Avoid: committing before handling; a producer per publish; one consumer group for all events; a key made of ticks; swallowing a handler exception; resolving handlers from the root provider; a `List<Task>` shared between threads; two serializers for one envelope.

---

## 4. `HsnSoft.Base.EventBus.Azure` (old)

### What it was
- Files: `EventBusAzure` (236 lines, real), `Connection/ServiceBusPersisterConnection` (55 lines) and `IServiceBusPersisterConnection`, and **three empty stubs** (`AzureConsumer`, `AzureProducer`, `Configs/AzureEventBusConfig`: 2 lines each: namespace + empty class). References: `Azure.Messaging.ServiceBus` 7.20.2, project `EventBus`.
- Behaviour: Service Bus **topic** named `ExchangeName`, one **subscription** per service named `ConsumerClientName`; on `Subscribe` it adds a `CorrelationRuleFilter { Subject = eventName }` rule and deletes the default rule; publish = `ServiceBusMessage { Subject = eventName, Body = same envelope JSON }`; processor `MaxConcurrentCalls = 10` hard-coded, `AutoCompleteMessages=false`.
- Defects read in code:
  - The processor builds the event name as `Prefix + Subject + Suffix` then trims again, but the lookup `GetEventInfoByName($"{prefix}{eventName}{suffix}")` uses the untrimmed key while the manager stores the trimmed key: with the default suffix `Eto` the lookup misses (indexer throws `KeyNotFoundException`, caught and logged as a warning): the handler would not run. (Inferred from code; not executed.)
  - A handler is resolved from the root `_serviceProvider` even though a scope is created on the line before.
  - A handler failure is logged as Warning and the message is **completed anyway** (`processed = true`): lost, no `FailedEto`.
  - Sync-over-async in the constructor and in `Dispose`; `ClassName` of `AzureEventBusConfig` empty, so `IOptions<AzureEventBusConfig>` has no `ConsumerClientName` of its own (it reads the base `EventBusConfig`, whose `ExchangeName` default `HsnSoft` would be a topic name).
  - `UserId`/`UserRoles` read from `ITraceAccesor` (different source than RabbitMQ, which reads `ICurrentUser`).
  - `fetchCount` and `isExchangeEvent` ignored.
- Never used by hhs (no `servicebus` string in hhs code or config).

### Where it is now
(d) **Missing**: no `src/HsnSoft.Base.EventBus.Azure`. Open item recorded in ADR-0003 (names Azure Service Bus as a sibling provider), ADR-0023 ("Not done yet"), ADR-0025 (`feat/new-framework` is not merged until Kafka and Azure Service Bus are done), `docs/adr/INDEX.md` open items, `docs/EventBus.md` "Not done yet". Azure *Blob* is ported (see section 5), Service Bus is not.

### Why
Recorded as "to be ported" (ADR-0023/0025); no ADR says dropped. Reason for the delay: inferred, no consumer needs it.

### Verdict
Flawed and unfinished. Not worth porting line by line.

### Gap
**Low.** No hhs or Hsx need. ADR-0025 makes it a blocker for merging `feat/new-framework` into `dev` only because it was promised; **the maintainer should either implement it or amend ADR-0025/0023 (new ADR that supersedes) to drop the promise** (open question 1). If added: `HsnSoft.Base.EventBus.AzureServiceBus` (the name `EventBus.Azure` also fits the old one) referencing only `EventBus` + `Azure.Messaging.ServiceBus`; topic = exchange, subscription = consumer, `SqlFilter`/`CorrelationRuleFilter` on `Subject` as the old code did; use the native Service Bus dead-letter queue and `MaxDeliveryCount` instead of `FailedEto`; use `SendAsync` = a queue per service. Reference check: sessions/ordering are not needed by the envelope.

### Keep & avoid
- Keep: the topic = exchange, subscription = consumer, correlation-filter-on-Subject mapping (it matches the RabbitMQ naming and keeps the envelope identical).
- Avoid: completing a message whose handler failed; stub classes left in a package; root-provider handler resolution; trimming the name twice with different keys.

---

## 5. `HsnSoft.Base.BlobStoring` and its `.Aws`, `.Azure`, `.FileSystem`, `.Minio` (old)

### What it was
- On `dev-backup` **every blob project is a csproj plus FodyWeavers files and no `.cs` at all** (checked: `git ls-tree -r dev-backup -- src/HsnSoft.Base.BlobStoring*`, and `git log --all --diff-filter=D` for deleted `.cs`: none; the history is only reference/version edits, from the first commit `ac7d3c2e`, which also had an `Aliyun` skeleton). They are an ABP-shaped placeholder. Declared references: `BlobStoring` -> Core; `Aws` -> BlobStoring + `AWSSDK.S3` 4.0.100.3; `Azure` -> BlobStoring + `Azure.Storage.Blobs` 12.29.1 + `System.Text.Json`; `FileSystem` -> BlobStoring + `Polly` 8.7.0; `Minio` -> BlobStoring + `Minio` 7.0.0. They were in `HsnSoft.Base.sln` and published as NuGet packages without public types.
- hhs evidence: only `video-generator` references `HsnSoft.Base.BlobStoring.Aws` (and its test project), and uses **no type of it**: `CdnBunnyS3Provider` builds `AmazonS3Client` directly and writes a hand-made **multipart upload** (8 MB parts, abort on failure). So hhs took the package only for the transitive `AWSSDK.S3`.
- Motto fit: nothing to judge in code; structure (abstraction + separate providers, each with the SDK it names) matches the motto, `Polly` on `FileSystem` was an odd choice.

### Where it is now
(a) Same names for BlobStoring, `.Minio`, `.Azure`, `.Aws`; written new in the rewrite:
- `BlobStoring`: `IBlobStore` (`SaveAsync(container,name,stream,contentType)`, `OpenReadAsync` returns `null` if absent, `DeleteAsync` idempotent) and `SeekableContent` (1 MB in memory, larger to a temp file, ADR-0112). Ref: Core only.
- `.Minio` (`MinioBlobStore`, `MinioDownload` streaming via pipe, options, health check, `AddMinioBlobStoring`), `.Azure` (`Azure.Identity` for host identity or connection string, `AddAzureBlobStoring`), `.Aws` (`AddAwsBlobStoring`, `ServiceUrl` for any S3-compatible store, role of the host). Each references only `BlobStoring`, its SDK, HealthChecks and Options.ConfigurationExtensions.
- One shared contract test class `BlobStoreContractTests` (ADR-0076); timeouts opt-in (ADR-0119); test image (ADR-0108).
- Edx uses `AddMinioBlobStoring` and `IBlobStore` (`EnvelopeStorage`); `hhs-analysis.md` rates storage as "Have".
- (d) `.FileSystem`: **missing**, and it never had code. `Aliyun`: dropped silently (skeleton).

### Why
ADR-0003 (provider split), ADR-0076 (Azure and AWS added), ADR-0112, ADR-0119, ADR-0108. Not porting `FileSystem`/`Aliyun`: inferred (no consumer).

### Verdict
Old: unimplemented skeleton, structure sound. Superseded by a real, tested implementation.

### Gap
- `.FileSystem`: **Low.** A local-disk `IBlobStore` would let a developer run a service without MinIO/Docker and would serve single-node deployments. If wanted: `HsnSoft.Base.BlobStoring.FileSystem`, references BlobStoring only, **no Polly**; must treat `container`/`name` as untrusted (path traversal: reject `..`, rooted names, separators), write to a temp file and move for atomic replace, stream reads. This is the one capability with a security trap.
- **hhs needs more than `IBlobStore`** for large objects: multipart/resumable upload with part size and abort, and presigned/public URL (video upload to S3-compatible CDN). `IBlobStore.SaveAsync(Stream)` covers upload through the SDK's own transfer logic (the Minio/AWS providers use seekable streams), but **not** presigned URLs or listing/prefix deletes. Hsx treats the CDN (Bunny) as a product provider (`hhs-analysis.md` line 68): **document only, Low.**
- Aliyun: **keep dropped.**

### Keep & avoid
- Keep: provider-per-SDK split; contract tests shared across providers; `null` for a missing blob.
- Avoid: publishing empty packages (the old nupkgs of these five were empty, and `Polly` on a FileSystem store made no sense); copying ABP's project list without code.

---

## 6. `HsnSoft.Base.Caching.StackExchangeRedis` (old)

### What it was
- Files: `Caching/StackExchangeRedis/{IRequestLimitStore, RedisRequestLimitStore, RedisLockService, RedisHealthCheck (class `CustomRedisHealthCheck`)}`, `Domain/Repositories/{IRedisRepository<T>, RedisRepository<T>}`.
- References: `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Caching.StackExchangeRedis`, `Microsoft.AspNetCore.DataProtection.StackExchangeRedis`, HealthChecks, project `Ddd.Domain` and project `Json` (project refs). **No registration extension, no `IDistributedCache` setup in the package**: hhs `AddHostingRedis` did it (`AddStackExchangeRedisCache` + its own `IConnectionMultiplexer` = two multiplexers, i.e. four connections per pod; ADR-0119 describes exactly this).
- `IRequestLimitStore`/`RedisRequestLimitStore`: INCR with TTL on first increment (race: `KeyExpire` after `INCR` can be skipped on a crash, leaving a key without TTL), DECR with clamp to 0 via a separate `SET` (non-atomic). A gateway/job concurrency counter.
- `RedisLockService`: `TryAcquireAsync` = `GetString` then `SetString` through `IDistributedCache` (**not atomic**: two callers can both acquire), no owner token, `ReleaseAsync` deletes whoever's lock, key built as `lock::key` (the prefix already ends with `:`, then `:{key}` is added). The hhs analysis marks it "registered, never used".
- `RedisRepository<T>`: typed JSON get/set/list/incremental-id helpers over `IDatabase`; Newtonsoft; **swallows every exception and returns `false`/`null`/`[]`** (a Redis outage looks like an empty cache); `SetAndGetIncrementalId` is `async` but calls the sync `StringIncrement`; `ListLeftPop` deserializes a possibly empty `RedisValue`; depends on `IAppConsoleLogger`; lives in namespace `HsnSoft.Base.Domain.Repositories` (not a repository in the DDD sense). hhs registers it as open generic singleton but grep finds **no injection of `IRedisRepository<>`** in hhs code: unused.
- `CustomRedisHealthCheck`: either connects to a connection string **per probe** (new multiplexer each time) or uses a given multiplexer; swallows per-endpoint exceptions; returns Healthy when no standalone server is found (clusters/sentinel always "ready").
- The `DataProtection.StackExchangeRedis` reference was unused in code (hhs has it commented out).
- Real hhs uses: `IRequestLimitStore` via `[EndpointConcurrencyLimit]`/`[EventConcurrencyLimit]` attributes and one handler (`TestQueryCompletedEtoHandler`), to stop two pods running the same scheduler-triggered job; `CustomRedisHealthCheck` in health checks; the `IDistributedCache` for permission grants.

### Where it is now
(a) Same name, much smaller: `src/HsnSoft.Base.Caching.StackExchangeRedis` (refs Core, `Microsoft.Extensions.Caching.StackExchangeRedis`, HealthChecks, Options.ConfigurationExtensions, `StackExchange.Redis` 3.4.20): `AddBaseRedis(configuration, connectionName)` registers `IDistributedCache` and a **single shared** `IConnectionMultiplexer` (`AbortOnConnectFail=false`), `RedisOptions` (`InstanceName`), `RedisHealthCheck` (tagged ready). The Ddd.Domain and Json references and Newtonsoft are gone. (c) intentionally dropped: `RedisLockService`, `IRequestLimitStore`/`RedisRequestLimitStore`, `IRedisRepository`/`RedisRepository`, the DataProtection reference.

### Why
ADR-0119 (one connection for cache and service, readiness checks the connection the cache uses), ADR-0081 (permission grants in the distributed cache), `docs/Overview.md` ("a product's gateway rate limit is that product's shared code, not a framework package"), `hhs-analysis.md` lines 42, 71, 91 (lock unused; concurrency limit not needed with compare-and-set claims). Dropping the repository: inferred (unused, swallows errors).

### Verdict
Cache registration: was missing entirely, now sound. The three extras: flawed (non-atomic lock, non-atomic limit counter, error-swallowing repository) and product-specific. Superseded.

### Gap
- Distributed lock: **keep dropped for now, Low.** `hhs-analysis.md` leaves "a lock stays a question for the daily jobs". If needed, do it right: `SET key token NX PX ttl` and a Lua compare-and-delete release (or RedLock-style only if multi-node); put it in a separate small package or in `Caching.StackExchangeRedis` as an opt-in registration that needs no new third-party reference (it already has `StackExchange.Redis`); name `IDistributedLock`. Users: Hsx daily jobs, schedulers that must run once across pods.
- Request limit store / rate limiting: **keep dropped** (product gateway code, Overview). A gateway rate limiter should use ASP.NET Core `RateLimiter` with a Redis-backed partition if cross-pod, in Hsx's public gateway (`hhs-analysis.md` item 4 of the Hsx plan).
- DataProtection key ring in Redis: **document only, Low.** Any multi-pod service that uses cookies/antiforgery needs a shared key ring; this is `PersistKeysToStackExchangeRedis` (one line, needs the `Microsoft.AspNetCore.DataProtection.StackExchangeRedis` package). It must **not** be in `Caching.StackExchangeRedis` (forced package); document it in `docs/Hosting.md`, or add a separate tiny package only if the Hsx auth server's HttpOnly refresh-token cookie turns out to need it.
- Typed Redis helpers (`IRedisRepository`): **keep dropped**; `IDistributedCache` plus the service's own `IConnectionMultiplexer` is the contract.

### Keep & avoid
- Keep: `INCR` + `EXPIRE` idea only inside a product limiter, if ever (make it one Lua script or `INCR` followed by `EXPIRE NX`); the health-check idea of pinging each configured endpoint (the new check is a single multiplexer ping: check it covers cluster/sentinel, **not verified here**).
- Avoid: lock via get-then-set; releasing a lock without an owner token; swallowing Redis exceptions into `false`/`null`; a new multiplexer per health probe; two multiplexers per pod; a cache package that references the DDD domain and a JSON library.

---

## hhs real use of the old event bus (evidence for sections 1-3)

- Packages referenced: `HsnSoft.Base.EventBus` (Contracts), `EventBus.RabbitMQ` and `Caching.StackExchangeRedis` (Shared.Hosting), `BlobStoring.Aws` (video-generator, unused types). No `EventBus.Kafka`, `EventBus.Azure`, no other blob package.
- 54 files use handler types (`IIntegrationEventHandler<>`/`ApplicationEventHandlerBase<>`), 103 `PublishAsync` calls, almost all `PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: ...)` (the `IEventApplicationService.SetParentIntegrationEvent` pattern, 26 files); the only use of `isExchangeEvent:false, isReQueuePublish:true` is the event-manager requeue (`FailedIntegrationEventAppService.cs:195`), which the new `SendAsync(..., isReQueuePublish: true)` replaces.
- The product re-implemented around the framework: an **inbox** (`Hhs.Shared.Contracts.EventInbox`, handler base class with idempotency), the generic `ReQueuedEtoHandler` (reflection to rebuild the envelope, sets `HopLevel+1`, `ReQueuedCount+1`), the DI wiring, and an event-manager service storing `FailedEto`. The new framework covers the inbox (ADR-0067) and exposes the pieces of the requeue (`IEventDispatcher`, `IFailedEventNotifier`, `IEventBusSubscriptionManager`, doc "Product side"); the `ReQueuedEtoHandler` stays a product class, as documented.
- Naming in production: exchange `HHS`, suffix `Eto`, queue `HHS_<Service>_<Event>`, error client `EventManagerService`, sections `RabbitMq:Connection` and `RabbitMq:EventBus`: all preserved by the new `EventBusOptions`.

## Open questions for the maintainer
1. **Azure Service Bus:** implement `EventBus.Azure` (native DLQ, topic = exchange, subscription = consumer), or supersede ADR-0023/0025 to say it is not planned? ADR-0025 currently makes it a condition for merging `feat/new-framework` into `dev`.
2. **Outbox:** hhs's own code calls the publish-after-commit loss a known gap, while `hhs-analysis.md` says "not needed". Decide before Hsx writes its first chain: an opt-in `EventBus` outbox decorator with EF Core and MongoDB providers (like the inbox), or accept the loss window and document it.
3. **`UserRoles` wire change (comma string to JSON array) and `Producer` value:** is a rolling mix of old and new services still possible anywhere? If yes, add a tolerant converter for `UserRoles` (accept string or array); if not, record in an ADR that the wire changed on purpose (ADR-0023 says "same wire", ADR-0057 changed it).
4. **`BlobStoring.FileSystem`:** wanted for local development and single-node deployments? If yes, accept the path-traversal rules as the contract.
5. **Distributed lock:** do the Hsx daily jobs need a lock across pods, or do compare-and-set claims on rows suffice (the hhs-analysis position)? If a lock, in which package.
6. **DataProtection on Redis:** does any Hsx service use cookies or antiforgery behind more than one pod? If so, document the one-line registration or add a separate package.
7. **Large-message Kafka settings** (old defaults 50 MB for message/fetch size): should `KafkaOptions` expose them? Not verified in the new code.
8. The `docs/adr/INDEX.md` open item "Port the Kafka consumer" is stale (Kafka is ported through ADR-0062..0066); correct it by the usual rule (a new ADR or INDEX note, never rewriting a decision).
