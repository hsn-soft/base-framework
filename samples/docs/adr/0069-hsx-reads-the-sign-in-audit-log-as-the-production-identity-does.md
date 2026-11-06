# ADR-0069: Hsx reads the sign-in audit log as the production identity does

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

[ADR-0068](0068-hsx-follows-the-production-identity-the-business-is-the-same.md) makes the identity service of Hsx the production one with the names of Hsx. The production identity service gained the read of its sign-in audit log (its ADR-0029), and the Hsx auth server already writes the audits.

## Decision

- The identity service of Hsx has the same read: `IAuthLoginAuditAppService` and `AuthLoginAuditsController` (`POST api/identity-service/v1/commercial/audit-logs/logins/get/{id}` and `.../paged-list`, `endpoint.audit-logs.read`). A caller reads the audits of its tenants (the system tenant all of them, the unknown users too), no further back than the days of `constraint.audit-logs.max-days`; a role without that constraint gets 403 before anything is read.
- The search is on `AuthLoginAudit.NormalizedUserNameOrEmail`, a normalized twin that **may be empty** (the audit is written for every attempt), with the migration `AddAuthLoginAuditNormalizedUserNameOrEmail`. A development database made earlier is migrated by it (its audits, if any, keep an empty twin) or dropped and made again.
- The code is the production code with the names changed; the sample has no tests of its own for it (the tests of a sample are written after the framework is done), and the production repository has them.

## Related

[ADR-0068](0068-hsx-follows-the-production-identity-the-business-is-the-same.md), [ADR-0066](0066-the-samples-follow-the-rules-of-the-normalized-fields.md).
