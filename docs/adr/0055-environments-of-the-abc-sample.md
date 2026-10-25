# ADR-0055: The environments of the Abc sample, and the names of its databases

- **Status:** Accepted
- **Date:** 2026-10-25 (changes the settings of [ADR-0051](0051-every-context-is-named-by-its-properties-class.md) and the database names of [ADR-0049](0049-abc-databases-and-provider-references.md), [ADR-0050](0050-a-context-has-a-properties-class-for-its-database.md) and [ADR-0053](0053-more-than-one-store-in-a-service.md))

## Context

Every host of the Abc sample (the gateways, the microservices, the auth server, the web apps) had one `appsettings.json` with the values of a developer's machine. A system runs in more than one place.

## Decision

- **Five environments, with names in small letters:** `development`, `docker`, `uat`, `stage`, `production` (the value of `ASPNETCORE_ENVIRONMENT`; the file names follow it, so they are found on a case sensitive file system too).
- **Two layers.** `appsettings.json` holds what is the same everywhere (the logging levels, the paths, the JWT issuer, the event bus names, the routes of a gateway). `appsettings.<environment>.json` holds what differs: `Urls`, `ConnectionStrings`, `RabbitMq:Connection`, the Graylog address and whether it is on,
  the origins of the services behind a gateway and its trusted proxies, the gateway url of a web app, and how much is logged.
- **`development` has the real values of a developer's machine** (localhost, the local containers); every host has a `Properties/launchSettings.json` with the profile `development`, so `dotnet run` and the IDE run in it. **The other four have fake values:** hosts like `prod-sql.abc.example.com` or `identity-service.uat.abc.example.com`
  (`docker` uses the names of a compose network: `sqlserver`, `postgres`, `mongo`, `redis`, `rabbitmq`, `http://order-service:8080`) and `CHANGE_ME` for every password and `abc_app` for every user. They are placeholders to be overridden by environment variables or a secret store; no real secret is ever written to a file.
  Production logs at `Warning`, the others at `Information`; Graylog is on in `uat`, `stage` and `production`.
- **The names of the databases are `Abc_<Context>_<Suffix>`:** the context is `Identity`, `History`, `Order`, `Product`, `Stock`, `Shipment`, `ShipmentHistory`; the suffix is `Dev` for `development` and `docker`, `Uat` for `uat`, `Stage` for `stage`, `Prod` for `production`
  (`Abc_Order_Dev`, `Abc_Identity_Prod`).

## Consequences

- A host that runs without an environment is in `production` (the default of ASP.NET) and cannot reach the fake servers: the launch profile, or `ASPNETCORE_ENVIRONMENT=development`, is how a developer runs it.
- The sample databases have new names: the old ones (`abc_order`, ...) are not used any more; the services create the new ones when they start.

## Related

[ADR-0051](0051-every-context-is-named-by-its-properties-class.md), [ADR-0053](0053-more-than-one-store-in-a-service.md), [ADR-0031](0031-rsa-keys-of-the-samples.md).
