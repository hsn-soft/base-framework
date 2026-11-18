# ADR-0161: A host open to the internet that has nothing for a search engine answers robots.txt with one call

- **Status:** Accepted
- **Date:** 2026-11-18

## Context

The hosts of a system that are open to the internet (the gateways, the auth server, a host that serves scripts to the sites of other companies, a login page) have nothing for a search engine, and a crawler that follows what it finds there does harm: it lists the login page and the addresses of the services, and a crawler that runs a script asks for what the script asks (the ads of a player, which spoils the numbers of the ad server). Each of these hosts wrote its own `MapGet("/robots.txt", ...)` with the same text; one of them (a single page app behind nginx) had none, and its fallback answered `/robots.txt` with the page of the app.

## Decision

`MapBaseRobots()` of `HsnSoft.Base.AspNetCore` maps `GET /robots.txt` and answers `User-agent: *` and `Disallow: /` as plain text, for anyone, left out of the API description. A host that is open to the internet and has nothing for a search engine calls it, as it calls `MapBaseHealth()`; a host that wants to be crawled does not and maps its own file. It is a request to well-behaved crawlers, not a protection. A host with no server code answers the same text from its own configuration.

## Consequences

One call and one text for every such host, and a test of it in one place. The hosts that wrote the line themselves replace it with the call when they take the version that has it (it is not a change of behavior). A host that needs another text (a crawlable part of a site) maps its own file, as before.

## Related

[Hosting](../Hosting.md), `BaseHostingExtensions.MapBaseRobots`, `test/HsnSoft.Base.AspNetCore.Tests/AppInfoTests.cs`.
