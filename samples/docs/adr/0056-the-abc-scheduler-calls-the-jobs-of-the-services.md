# ADR-0056: The Abc scheduler calls the jobs of the services

- **Status:** Accepted
- **Date:** 2026-11-01 (applies *framework ADR-0126*, see `../../../docs/adr/0126-the-endpoints-a-scheduler-calls-are-authenticated-by-a-key-the-host-requires.md`)

## Decision

- **`Abc.Scheduler.Host`** (Quartz) is the tenth host of Abc: it calls the jobs of `Scheduler:Jobs` (name, service, path, method, cron, `TriggerOnStartup`, timeout) on the services of `Scheduler:Services` (addresses by name), with the key of the scheduler
  and the headers `SchedulerJobId` (also the correlation id) and `SchedulerJobName`. A section that cannot work (a service that is not listed, a wrong cron or path, a name used twice) stops the host at its start with every reason. A run that fails is logged
  and the next one goes on; two runs of a job never overlap.
- **One pod, never two:** in the cluster the scheduler is `replicas: 1` with the `Recreate` strategy (a rollout stops the old pod first), the one exception to the two pods of every host.
- **The first job** is `POST /api/shipment-service/v1/commercial/jobs/payment-summary` of the shipment service (`[SchedulerAuth]`, `[AllowAnonymous]`): it counts the payments of each status and logs it, a job that only looks.
- **The key:** `development` settings have a development value (`development-scheduler-key`, like the development passwords); the `docker`, `uat` and `production` settings say `CHANGE_ME` and `render-settings.sh` puts `SCHEDULER_API_KEY` of `deploy/.env` in (made by `make-env.sh`, git ignores it), so the same random key is in the scheduler and in the shipment service. A `.env` file or
  `Scheduler__ApiKey` overrides any of them.
