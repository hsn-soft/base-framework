# ADR-0054: `Program.cs` chooses the health endpoints and the event bus (with its handlers), and each service has its Swagger settings

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0052](0052-the-hosts-start-with-add-base-hosting-and-run-base-async.md) and [ADR-0053](0053-each-service-has-its-own-audience-and-the-program-says-how-it-authenticates.md))

## Context

`AddAbcHosting` registered the health checks and mapped the health endpoints for every host, so a service could not be without them; the handlers of the event bus were added by a second call (`AddEventHandlers`) that sat far from the
`AddAbcEventBus` it belonged to, and a reader could not tell whether it was for the fetch counts or for the registration (it is for both: it scans the assembly of the service and says how many messages of an event are handled at once). Swagger was
configurable per service (framework ADR-0024) but no service said where it is on.

## Decision

- **Health is a choice of the host:** `AddBaseHealthChecks(configuration)` and `app.MapBaseHealth()` are in `Program.cs`; a service that has no health endpoints leaves both out. The shared hosting no longer has them.
- **The event bus and its handlers are one call:** `AddAbcEventBus(configuration, handlersAssembly, fetchCounts)` (`AddEdxEventBus` for Kafka). The assembly is what is scanned for handlers (a service that only publishes gives none), `fetchCounts`
  names the events handled several at a time. A service with no broker does not call it; the handlers are the service's own, so the call takes them from it.
- **Swagger is in the settings of every host that serves a document:** the `Swagger` section (`DisabledIn`, `RoutePrefix`) is written in `appsettings.json` with the environments it is off in (`Production`, `Staging`, `stage`, `uat`); a service changes
  its own list, or says `Enabled` for all environments.
- **The commands sent to a database** are logged by `EntityFramework:LogCommands` and `Mongo:LogCommands` (*framework ADR-0124*): `false` in `appsettings.json`, `true` in the `development` settings of each service that has the database.
