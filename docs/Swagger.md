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
