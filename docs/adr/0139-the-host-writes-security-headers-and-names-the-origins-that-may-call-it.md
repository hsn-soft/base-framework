# ADR-0139: The host writes security headers and names the origins that may call it

- **Status:** Accepted
- **Date:** 2026-11-03

## Context

The old framework wrote no security headers and had no CORS setting; the hhs system allowed any origin with credentials, which lets a page of any site call its services with the signed-in user's cookies. The new host ([ADR-0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md)) has limits and proxies but neither. The Edx sample wrote a correct CORS policy of its own, in its shared hosting; every product would write one again, some wrongly ([the analysis](../old-framework/4-web-tools.md)).

## Decision

- **`UseBaseSecurityHeaders()`** in `HsnSoft.Base.AspNetCore` writes `X-Content-Type-Options: nosniff`, `X-Frame-Options` (default `DENY`), `Referrer-Policy` (default `no-referrer`), `Strict-Transport-Security` (one year; only on an https request and outside Development, so a machine served over http is never locked out) and, when the settings give them, `Content-Security-Policy` and `Permissions-Policy`. It is on by default, from `Hosting:SecurityHeaders`, and a header the answer already has is left as it is.
- **CORS is a setting that names origins** (`Hosting:Cors`), registered by `AddBaseHosting` and applied by `UseBaseCors()`. With no origin listed no other origin may call. An origin is a scheme, a host and a port with no path; `*` is allowed only without credentials, and a setting that cannot be right stops the start (the host starts safely, ADR-0120).

## Consequences

A product gets the headers by one call and a policy by listing its origins; a page that must be framed or loads from other hosts changes a setting. A web host that serves behind a proxy needs the forwarded headers on for HSTS to see https. The Edx sample keeps its own policy until it is moved to the setting.

## Related

[ADR-0120](0120-the-host-takes-its-settings-from-one-section-and-starts-safely.md), [Hosting](../Hosting.md), [the old framework analysis](../old-framework/README.md).
