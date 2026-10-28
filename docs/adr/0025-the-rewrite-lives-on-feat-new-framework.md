# ADR-0025: The rewrite lives on `feat/new-framework` and is not merged into `dev` until the whole framework exists

- **Status:** Accepted
- **Date:** 2026-10-25

## Context

The framework is being rewritten from the ground up (single-capability packages, a new logging model, a new web api shell, ports of the event bus, ...), while the services in production still use the published packages built from `dev`.
Merging half a rewrite into `dev` would put an unfinished framework on the line that feeds `rel/nuget`. During the work, branches were stacked one on another (`feat/rewrite-core`, `feat/signer-sample`, `feat/eventbus-rabbitmq`,
`feat/logging`), and pull requests per branch would each have contained the ones before.

## Decision

- **All the work of the rewrite is collected on `feat/new-framework`.** It contains every earlier branch (they are its ancestors; they are only pointers now and are not worked on). New work, including the documents and the
  ADRs, is committed there.
- **`feat/new-framework` is not merged into `dev` until the whole new framework exists**: every package is ported and tested (the Kafka and Azure Service Bus brokers are the ones still open, see [ADR-0023](0023-event-bus.md)). Until then `dev`
  and `rel/nuget` stay as they are, so what the services use is not disturbed.
- The branch is pushed to `origin` regularly so the work is not only on one machine.
- **The CI runs on pull requests into `dev`** ([ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md)), so a push to `feat/new-framework` alone runs nothing. To see the CI (the Linux runner, Docker, the coverage gate) on the branch, open a **draft pull request into `dev`
  and do not merge it** (or run the workflow by hand on the branch); keep running the detector locally in the meantime ([ADR-0014](0014-detector-before-a-pull-request.md)).
- Short-lived branches for a piece of the work, when wanted, branch from `feat/new-framework` and come back into it, not into `dev`.

## Consequences

- The branch is far ahead of `dev` and keeps growing; there will be one large merge at the end, reviewed against these ADRs rather than commit by commit.
- A fix needed in the old framework for production goes to `dev` separately and has to be brought into the rewrite by hand when it is still relevant.
- The first Linux CI run of the rewrite happens only when the draft pull request is opened; problems that only Linux shows (path case, Docker) are still ahead.

## Related

[ADR-0001](0001-record-architecture-decisions.md), [ADR-0007](0007-build-settings-and-versioning.md), [ADR-0012](0012-ci-workflow-and-what-breaks-a-pull-request.md), [ADR-0014](0014-detector-before-a-pull-request.md), [ADR-0023](0023-event-bus.md).
