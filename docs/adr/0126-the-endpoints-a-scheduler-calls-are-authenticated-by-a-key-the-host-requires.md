# ADR-0126: The endpoints a scheduler calls are authenticated by a key the host requires

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md))

## Context

The old system ran its recurring work with a scheduler (Quartz, a host of its own) that called `jobs` endpoints of the services over HTTP, each guarded by an attribute that compared a header with a key in the settings. The key was in
`appsettings.json`, in `.env` files and in manifests of the repository, the comparison was not constant-time, a missing header and a wrong one answered differently (400, 401), a service with no key set failed at the first call, not at the start,
and the code that did it was copied into every service.

## Decision

- **`[SchedulerAuth]`** (`HsnSoft.Base.AspNetCore.WebApi`) marks an endpoint the scheduler calls. The caller proves itself with the key of the scheduler in the header `SchedulerApiKey` (`SchedulerHeaders`), compared with `Scheduler:ApiKey` in constant time;
  a missing, empty or wrong key is one 401 whose answer has neither the key nor what was sent. The key is the authentication of the job, so the controller says `[AllowAnonymous]`; a user's token is not accepted in its place.
- **`AddBaseSchedulerAuth(configuration)`** reads `Scheduler:ApiKey` and **requires** it (ADR-0120): a service that has job endpoints does not start without the key.
- **The key is a secret of the platform, not of the repository:** the environment (`Scheduler__ApiKey`), a `.env` file or a secret; the same key is in the scheduler and in every service it calls.
- `SchedulerJobId` (also sent as the correlation id) and `SchedulerJobName` are the other headers of a job.
- The scheduler itself is a **product's host**, not a framework package (what is scheduled, and where, is the product's).

## Consequences

- A service gets endpoints for a scheduler with one attribute and one call, with the key checked the same way everywhere and a start that fails when it is missing.
- The header names of the old system are kept, so its scheduler and its services can move to this framework one at a time.
- Not here: more than one key (a key per scheduler or per job), a signature of the request (the call goes in the cluster network), the lock of a job that must not overlap on two pods.

## Related

[ADR-0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md), [docs/Scheduler.md](../Scheduler.md).
