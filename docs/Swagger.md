# Swagger

`HsnSoft.Base.AspNetCore.Swashbuckle` serves the Swagger document and its UI (Swashbuckle) with one call to register and one to use. It depends only
on `HsnSoft.Base.Core`; `HsnSoft.Base.AspNetCore.WebApi` does not know it, so a service takes Swagger only if it wants it.

```csharp
// registering
services.AddBaseSwaggerWithBearer("Orders API", "Enter a valid token", configuration);   // a button that sends a JWT as "Authorization: Bearer"
// services.AddBaseSwagger("Gateway API", configuration, name: "gateway");                  // no authorization button
// services.AddBaseSwaggerWithOAuth("Orders API", "https://identity", new Dictionary<string, string> { ["orders"] = "Orders" }, configuration);

// using, before authentication
app.UseBaseSwagger();   // /swagger (the UI) and /swagger/v1/swagger.json (the document)
```

Every endpoint is in the document and the types are named by their full name (two types of one name in two namespaces do not collide). Anything else
Swashbuckle can do goes in the last parameter: `configure: options => options.IncludeXmlComments(...)`.

## Where it is served

Swagger is for developers: it is **off where the service runs for real** (`Production`, `Staging`, `stage`, `uat`), by the same code in every
environment, so no `if (!IsHostProduction())` is repeated in `Program.cs`. The `Swagger` section decides (when it is not given, the rule above):

| Setting | Meaning |
|---|---|
| `Enabled` | `true` serves it everywhere, `false` nowhere |
| `DisabledIn` | the environments that serve nothing (a list; when empty, the four above) |
| `RoutePrefix` | the path of the UI, `swagger` by default |

`/swagger` is already in `RequestLogging:ExcludedPaths`, so opening the UI writes no request logs.

## One host with Swagger and another without

Swagger is a call, not a layer: `HsnSoft.Base.AspNetCore.WebApi` does not know it, so a host that never calls `AddBaseSwagger...` and `UseBaseSwagger` has no Swagger, and no other package notices. Two ways to leave it out of a host:

- **By settings** (the project still references Swashbuckle): `"Swagger": { "Enabled": false }` in the host's settings; it is registered and serves nothing, whatever the environment.
- **By composition** (the project does not reference it): the host does not call it. That decision is the product's shared hosting project's: where it calls Swagger for every host (the Abc shared shell does), a host cannot opt out without a parameter in it
  (`AddAbcHosting(..., useSwagger: false)`, as there already is for `authenticateUsers`) or without moving the two calls out of the shell into the hosts, so each host chooses. Then the gateway that wants the UI of its services calls `AddBaseSwaggerGateway` and the one that does not calls nothing.

## A gateway and its services

Every service serves its own document (`UseBaseSwagger`), and the gateway shows them all in **its** Swagger UI, each as if it were the gateway's: the
paths and the address of the gateway, so "Try it out" goes through the gateway. That is `HsnSoft.Base.AspNetCore.SwaggerGateway`, in the gateway only:

```csharp
services.AddBaseSwaggerWithBearer("Gateway API", "Enter a valid token", configuration, name: "gateway");
services.AddBaseSwaggerGateway(configuration);      // reads SwaggerGateway
app.UseBaseSwagger();                                // the same call: the UI lists the gateway's document and the services' too
```

```json
"SwaggerGateway": {
  "PublicOrigin": "https://api.example.com",
  "Services": [
    { "Key": "identity", "DisplayName": "Identity API", "Origin": "http://identity:8080",
      "DownstreamPathPrefix": "/api/identity-service/v1/commercial", "GatewayPathPrefix": "/identity-service/v1" }
  ]
}
```

- `Origin` is the **internal** address of the service: the gateway fetches `/swagger/v1/swagger.json` from there (`SwaggerPath` changes it); `PublicOrigin` is the
  address clients use, written in every document (when it is not given, the address the document was asked at).
- `DownstreamPathPrefix` / `GatewayPathPrefix`: the paths the service has start with the first and the gateway has the second (the route that forwards
  `/identity-service/v1/...` to `/api/identity-service/v1/commercial/...`, see [ADR-0026](adr/0026-route-pattern-and-jwt-by-default.md)). Both empty when the gateway forwards the paths as they are. **With a `DownstreamPathPrefix`, the paths of the service that are not under it are left out** of the gateway's document: they are not forwarded by this gateway (the service's `public` endpoints on the commercial gateway, and the other way round).
- The rewritten document is kept `CacheDurationSeconds` (60); a temporary failure of a service is tried `RetryCount` (2) times more; a service that is down is
  a 502 where its document should be, the other documents are not affected. The settings are checked when the host starts.
- The documents are at `/swagger/services/{Key}/swagger.json`, behind the same rule as the UI: nothing in production-like environments. Who may open `/swagger`
  is the gateway's business (put its authorization before `UseBaseSwagger`).
- A landing page with the live state of every service, as the old gateway had at `/docs`, is not here: it is a product's page, built on the same settings.

## Coming from the old `HsnSoft.Base.Swashbuckle`

| Before | Now |
|---|---|
| `SwaggerConfigurationHelper.ConfigureWithBearer(services, desc, title)` inside `if (!IsHostProduction())` | `services.AddBaseSwaggerWithBearer(title, desc, configuration)` |
| `SwaggerConfigurationHelper.Configure(services, title, "v1", "gateway")` | `services.AddBaseSwagger(title, configuration, name: "gateway")` |
| `AddBaseSwaggerGenWithOAuth(authority, scopes)` | `AddBaseSwaggerWithOAuth(title, authority, scopes, configuration)` |
| `app.UseSwagger(); app.UseSwaggerUI(c => { c.RoutePrefix = "swagger"; c.SwaggerEndpoint(...) })` inside the same `if` | `app.UseBaseSwagger()` |
| `services.AddOpenApi()` (the built-in document) | gone: one document, the Swagger one |

The remote stream content mapping (`RemoteStreamContent` as `binary`) is not here: the type does not exist in the framework any more. A product that has
its own type for downloads maps it with `configure: options => options.MapType<...>(...)`.
