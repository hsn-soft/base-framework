# ADR-0121: A process knows who it is (`AppInfo`, `GET /`), the minimum of the thread pool is a setting, and the handlers of the event bus have a limit

- **Status:** Accepted
- **Date:** 2026-10-31 (adds to [ADR-0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md))

## Context

Nothing told a person or a probe which service, which instance and which version answered at an address. The old system had a static `ApplicationIdentifier` filled from a marker type (its name was the namespace of the
marker without the first segment) and a one-line plain text at `/`; the static made the id impossible to replace in a test and the name depended on a naming convention. The thread pool starts with as many threads as there
are cores and adds one about every half second when all are busy: a service that waits on many calls at once (consumers, blocking code) is slow for seconds after a burst, and nothing in the settings could change it.

## Decision

- **`AppInfo`** (a record: `Name`, `Id`, `Environment`, `Version`, `StartedAt`) is made once by `AddBaseHosting` and registered in the container, so anything that needs it asks for it. `Name` is `Service:Name`, else the name of
  the application; `Id` is the first of `APP_ID`, `POD_NAME`, `HOSTNAME`, else a new id (two pods of a service have two ids); `Version` is `BUILD_NUMBER` when the pipeline sets it, else the version of the assembly.
- **`MapBaseInfo`** maps `GET /` (anonymous, JSON, not in the Swagger document) with that record. A host that has a page at `/` maps its own instead. The shared hosting of the samples maps it for the services and the gateways.
- **`Hosting:ThreadPool`** (`MinWorkerThreads`, `MinCompletionPortThreads`): the minimum of the pool is **raised** to the setting at the start and never lowered (a lower value than the one in force is ignored). The services of the
  samples ask for 32 each. The maximum is not touched: the runtime knows it better than a setting.
- **`EventBus:MaxConcurrentHandlers`** (`EventBusOptions`, for both brokers): how many messages the service handles at the same moment whatever their events. Every event has its own `FetchCount` and a service with
  many events adds them up (a hundred consumers of five are five hundred handlers at once, against a pool of database connections of a hundred). The limit is a place in the dispatcher: a message that arrives when
  all places are taken waits for one without holding a thread (it is not lost, the broker counts it as taken) and a stop of the service ends the wait. 0 (the default) is no limit, as before; the services of the samples say 64.

## Consequences

- `GET /` answers who the instance is, for a person with a browser and for a platform.
- The id of an instance and the version of a build are the same everywhere they are shown, and a test can make its own.
- A service with many consumers can have its threads ready without a change of code.
- Not here: the pool limits of Mongo and Redis in the settings (next), and the use of `AppInfo` by the logs.

## Related

[ADR-0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md), [docs/Hosting.md](../Hosting.md).
