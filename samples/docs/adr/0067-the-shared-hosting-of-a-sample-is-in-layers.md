# ADR-0067: The shared hosting of a sample is in layers

- **Status:** Accepted
- **Date:** 2026-11-05

## Context

[ADR-0003](0003-shared-hosting-projects.md) made `{Product}.Shared.Hosting` what every host of a sample has, but it carried what only a web host has: the web api shell, Swagger, the bearer token and the ASP.NET Core reference. The framework has now decided how a product's shared hosting is layered (framework ADR-0151), and the production system has done it (`Hhs.Shared.Hosting`, `Hhs.Shared.Hosting.Http`).

## Decision

In Abc, Edx and Hsx: `{Product}.Shared.Hosting` has the logging only (`Add{Product}Logging`, no ASP.NET), and the new `{Product}.Shared.Hosting.Http` has what every web host has (`Add{Product}HttpHosting`, `Use{Product}HttpHosting`, `Add{Product}Authentication`, `Add{Product}Authorization`, and the public key of the tokens). `.Gateways` and `.Microservices` build on `.Http`, and a host that calls the web composition directly (the auth servers, the WCF hosts) references `.Http`. The message broker, Redis and a database stay the choice of each host (ADR-0009, ADR-0010). The public key `public_key.xml` (made by `generate-rsa-keys.cs`, not in git) now lives in `{Product}.Shared.Hosting.Http`; the scripts and the READMEs name the new path.

## Consequences

A host of another kind (an agent) can take the core without ASP.NET. The calls were renamed (`AddAbcHosting` is `AddAbcHttpHosting` and so on) and a host that calls them or `Add{Product}Authentication` has `using {Product}.Shared.Hosting.Http;`. A local `public_key.xml` made before this change is moved with the project (it is moved to the new folder where it was found).

## Related

[ADR-0003](0003-shared-hosting-projects.md), framework ADR-0151, [ADR-0027](0027-grpc-and-wcf-are-entry-points-of-a-host.md).
