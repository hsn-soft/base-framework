# Cluster D: web, hosting, logging, tools

Scope: old `src/HsnSoft.Base.{AspNetCore, AspNetCore.Mvc, AspNetCore.Mvc.UI, Swashbuckle, Http.Client, UI, UI.Navigation, PuppeTeer, Serilog}` on `dev-backup`, the old health checks and middleware, and `hhs/shared/src/Hhs.Shared.Hosting*`. They are compared with the new `AspNetCore`, `AspNetCore.WebApi`, `AspNetCore.Swashbuckle`, `AspNetCore.SwaggerGateway`, `Hosting`, `Logging`, `Logging.Serilog` and `Logging.NLog`, and with the docs Hosting.md, Health.md, WebApi.md, Swagger.md and Logging.md.

How I checked: I read every old `.cs` file of the nine projects. In hhs I grepped for the namespaces and types. In the new tree I read the code, not only the names. Commit `0c82c90e` ("start the rewrite from Base and Core only", 2026-10-22) deleted every old project except `Base` and `Core`, so the history has no per-project reason for any drop. Every "why" below is cited from an ADR where one exists. Otherwise it is marked **inferred**.

## Summary

| Old project | Now | Verdict | Gap / priority |
|---|---|---|---|
| `AspNetCore` | Split and rewritten: `AspNetCore` (host, health, `.env`, `AppInfo`), `AspNetCore.WebApi` (answers, exception handler, request log, language), `Hosting` (worker), `Authentication` (current user and principal) | Old design flawed (heavy coupling, sync-over-async workers, thread-culture middleware) | Covered. Small gaps: security-headers middleware (Low/Medium), CORS from settings (Low/Medium) |
| `AspNetCore.Mvc` | Merged: `ApiControllerBase` into `WebApi`. Anti-forgery is the built-in `AutoValidateAntiforgeryToken`. `RazorRenderService` and `BaseController` dropped | Superseded | None. Document the web-UI recipe (Low) |
| `AspNetCore.Mvc.UI` | Dropped | Flawed (empty shell, AutoMapper and NUglify forced) | None |
| `Swashbuckle` | `AspNetCore.Swashbuckle` (+ `SwaggerGateway`) | Sound idea, flawed implementation; superseded | Covered |
| `Http.Client` | **Missing** (dropped by omission). The old package was a thin and partly harmful wrapper | Flawed | **Add a small package (Medium-High)**. Copy-pasted 3 times in the samples (see section 5) |
| `UI` | Dropped | Dead code (an ABP leftover) | None |
| `UI.Navigation` | Dropped. Menu is data in Hsx (samples ADR-0062) | Dead code, no consumer | None |
| `PuppeTeer` | **Missing** (dropped by omission; Hsx analysis says "Product") | Sound core idea (lease and limiter), unsafe defaults | **Add an opt-in package (Medium)**. Only text-normalizer uses it; it needs a hardened rewrite |
| `Serilog` | `Logging` (abstractions, masking, settings) + `Logging.Serilog` (+ `Logging.NLog`) | Sound but over-coupled and static-heavy; superseded | Covered |

Highest findings:
1. `Http.Client` is a real gap, but not in the old shape. Abc and Hsx each hand-write the same `GatewayRequestHandler` plus a `GatewayClient` that unwraps `BaseResponse`, with no resilience and no timeout setting. hhs never used the package's types at all.
2. `PuppeTeer` was used by exactly one hhs service (text-normalizer scraping). A new package is justified only as opt-in and hardened. Its old defaults (`--no-sandbox`, `--ignore-certificate-errors`, runtime browser download) are security smells.
3. Everything else in the cluster is covered, and mostly improved: health live/startup/ready, host limits, forwarded headers, request language, masking.
4. A security-headers middleware and a settings-driven CORS policy are the only small web-host leftovers. hhs had CORS "any origin + credentials", which is a security flaw. Edx's gateway already shows the right shape (a product's `AllowedOrigins`).
5. Minor doc inconsistency: `docs/Swagger.md` says Swashbuckle "depends only on `HsnSoft.Base.Core`", but its csproj and ADR-0071 reference `AspNetCore`.

---

## 1. HsnSoft.Base.AspNetCore

### What it was
Host helpers, a response format and request logging for an ASP.NET Core web api. Public types (`dev-backup:src/HsnSoft.Base.AspNetCore/HsnSoft/Base/AspNetCore/...`):
- `BaseAspNetCoreServiceCollectionExtensions`: `AddBaseAspNetCoreContextCollection` registers `ICurrentPrincipalAccessor`, `ICurrentUser`, `ICurrentTenantAccessor`, `ICurrentTenant`, `IWebClientInfoProvider` and two middlewares. `AddBaseAspNetCoreJsonLocalization` hard-codes the cultures `en, tr, ru`.
- `Hosting/Worker`: `BaseSingleThreadBackgroundService<T>` and `BaseMultiThreadBackgroundService<T>` (a loop with a "wait N seconds, can skip the wait" model, `IsProcessing` flag, `WaitContinuousThread`), plus `Test*BackgroundService` classes shipped in the package.
- `Hosting/Loader`: `LoaderHostedService` and `IBasicLoader`/`DefaultBasicLoader` (run a seeder or loader once at start).
- `Localization/BaseLocalizationMiddleware`, `Security/BaseSecurityHeadersMiddleware` (nosniff, X-XSS-Protection, X-Frame-Options), `Security/Claims/BaseClaimsMapMiddleware` (maps claim types) and `HttpContextCurrentPrincipalAccessor`.
- `Responses/*`: `UnifiedApiResponseFilter` (wraps results in `BaseResponse`), `ApiResponseWriter`, `StatusMessageProvider`.
- `Logging/RequestResponseLogger` (a `PersistentLogger` of its own) and `Settings/HostingSettings` (`IsEnabledRequestResponseLogger`, body size limits).
- `Tracing/CorrelationExtensions` (`UseCorrelationId`, `X-Correlation-Id`), `ClientExtensions` (`X-Channel`, `X-Client-Version`, `X-Client-Request-Lat/Long`), `HttpContextTraceAccessor` and `WebClientInfo/*` (user agent, remote IP).

References: Serilog.AspNetCore, JwtBearer, System.IdentityModel.Tokens.Jwt, and projects Auditing, Authorization, ExceptionHandling, Security, Serilog, Validation, VirtualFileSystem. Motto: **broken**. A plain web host forced JWT, Serilog, Auditing, Authorization and a virtual file system on every consumer. It also registered tenant and user services nobody asked for (ADR-0074 says tenants are opt-in).

Quality issues in the code:
- `BaseLocalizationMiddleware` sets `Thread.CurrentThread.CurrentCulture`. This does not follow `await` continuations and leaks onto pool threads. It calls `CultureInfo.GetCultures(AllCultures)` (about 800 objects) on every request. It reads only the first `Accept-Language` token and ignores `q` values.
- The workers do sync-over-async (`OperationAsync(...).GetAwaiter().GetResult()` inside `new Task(...)`, started with `Parallel.For`). `Dispose` and `StopAsync` use `Thread.Sleep` and busy loops. The flags are not volatile. The loop wakes once a second. Exceptions are logged by `ex.Message` only (no stack). `_workers` is mutated from `Dispose`.
- `LoaderHostedService` has a `while ... break` that never loops. It swallows `OperationCanceledException` and then logs "FAILED". It also runs the seeder inside `StartAsync`, which blocks startup.
- `BaseSecurityHeadersMiddleware` sends `X-XSS-Protection: 1; mode=block`, which is deprecated and can itself open leaks in old browsers (OWASP says `0` or omit). It has no HSTS, `Referrer-Policy`, CSP or `Permissions-Policy`.
- `BaseClaimsMapMiddleware` adds a new identity on every request and does `Maps.Keys.Contains` on a dictionary through LINQ.
- `HttpContextTraceAccessor.GetUserId` throws through `Check.NotNull` when there is no HttpContext (any background call).
- `UnifiedApiResponseFilter` writes `TraceId = HttpContext.TraceIdentifier`, not the W3C trace id.

### Where it is now
(b) Moved and rewritten, piece by piece:

| Old | New |
|---|---|
| Host settings, `HostingSettings` | `AspNetCore`: `AddBaseHosting`, `BaseHostingOptions` (section `Hosting`), `RunBaseAsync`, `RequireBaseSettings`, `AppInfo` + `MapBaseInfo` (`GET /`), `EnvFile` (`.env`), thread-pool minimum, Kestrel limits, `UseBaseForwardedHeaders` with known proxies |
| (none, hhs had its own) health | `AspNetCore`: `AddBaseHealthChecks`, `AddBaseReadyCheck<T>`, `MapBaseHealth` (`/health/live`, `/startup`, `/ready`) |
| `UnifiedApiResponseFilter`, `ApiResponseWriter`, `StatusMessageProvider` | `AspNetCore.WebApi/Responses/*` with the same names. Plus `GlobalExceptionHandler`, `ApiExceptionMapper`, `ModelStateTranslator`, `FileResultLogFilter`, `ResponseTrace` (W3C trace id) |
| `RequestResponseLogger`, `HostingSettings.*Logger*` | `WebApi/RequestLoggingMiddleware`, `RequestLoggingOptions`, `IRequestLogEnricher`. Written through `ILogger` |
| `UseCorrelationId`, `HttpContextTraceAccessor`, `ClientExtensions` | The correlation id and client headers live in `RequestLoggingMiddleware` (the incoming id is kept, length-limited to 128 and echoed in the response; a request without one takes the `TraceId`). The client headers are in `RequestLoggingOptions`. The event bus copies `ClientChannel/Version/Lat/Long` (`EventDispatcher.s_clientHeaders`) |
| `BaseLocalizationMiddleware`, localization registration | `WebApi/RequestLanguage.cs` (`IRequestLanguageResolver`: token claim, then `Accept-Language` by `q`, then default) + the `Localization` package |
| `LoaderHostedService`, `IBasicLoader` | Replaced by startup steps and seeders (ADR-0118, ADR-0125, ADR-0131), run one pod at a time under a lock |
| `BaseSingle/MultiThreadBackgroundService` | `Hosting/PeriodicBackgroundService` (`PeriodicTimer`, a failed run is logged, the loop goes on) |
| `HttpContextCurrentPrincipalAccessor`, `ICurrentUser` | `Authentication` (ADR-0036: the JWT authentication registers `ICurrentUser`) |
| `WebClientInfo/*` | Not carried. The request log writes the user agent and IP |
| `BaseSecurityHeadersMiddleware` | **Missing** (no equivalent) |
| `BaseClaimsMapMiddleware` | Dropped (no consumer in hhs). `Authentication` has claim-name helpers |

### Why
- ADR-0002/0003/0070/0071: one capability per package, and `X.Y` is a layer on `X`.
- ADR-0036: `ICurrentUser`.
- ADR-0074: tenants are opt-in.
- ADR-0117: health.
- ADR-0120: host limits, `.env` and a safe start.
- ADR-0121: `AppInfo` and the thread pool.
- ADR-0044: validation.
- ADR-0015 to 0021: logging.
- The loader, worker and security-header middleware drops have no ADR (**inferred**: no consumer, or a better built-in).

### Verdict
Flawed as built (a god package that forced Serilog, JWT and Auditing, with blocking workers and thread-culture middleware). Superseded by a smaller `AspNetCore` host package and a `WebApi` layer, which are better: they depend only on Core, read settings from one section, and have no static state.

### Gap
- **Security headers: add or fold, priority Low/Medium.** hhs never used `BaseSecurityHeadersMiddleware` (grep: zero hits), and Hsx and Abc have no header middleware at all. The back-office web hosts (`Hsx.CommercialWeb.Host`, a cookie-auth Razor app) have no `nosniff`, `X-Frame-Options`, `Referrer-Policy`, or HSTS. A modern version would live in `AspNetCore` as `UseBaseSecurityHeaders()` with a `Hosting:SecurityHeaders` section (nosniff, `X-Frame-Options: DENY`, `Referrer-Policy`, optional HSTS and CSP, no `X-XSS-Protection`). It is small, depends on nothing, and any web product uses it unchanged (ADR-0005 test 1). Alternatively, document it as a product choice.
- **CORS from settings, Low/Medium.** hhs `AddDefaultCorsSettings` allows any origin with credentials (`SetIsOriginAllowed(_ => true)` + `AllowCredentials`). That is an actual vulnerability, used in three hosts. Edx has a proper allowed-origins policy in `Edx.Shared.Hosting.Gateways`, and Hsx's analysis wants "allowed origins in the customer's settings". A `Hosting:Cors` section (explicit origins, never wildcard with credentials) could fold into `AspNetCore`. Today it is product code; decide whether a third product repeating it justifies the move (ADR-0005 needs a second product).
- **"Run now" and parallel workers: None.** The multi-thread worker had zero consumers. The single-thread one had two (permission sync workers), obsolete since permissions moved to the cache (Hsx analysis).
- **Durable work-item / retry worker: not this cluster.** `docs/hhs-analysis.md` section 5 lists it as the one real hosting gap. If built, it belongs next to `Hosting`.

### Keep and avoid
Keep:
- The idea of a "skip wait period" signal for a periodic worker (`SkipOperationWaitPeriod`), if a run-now trigger is ever needed. Note the old code's one-second polling is the wrong implementation.
- `UnifiedApiResponseFilter`'s case table. It is already ported with the same name.

Avoid:
- Thread culture in middleware. Use an async-flow accessor.
- Sync-over-async in workers.
- `Thread.Sleep` in `Dispose` or `StopAsync`.
- Test classes shipped in a production package.
- Registering tenant or user services from the web package.
- `X-XSS-Protection: 1`.
- Hard-coded supported cultures.

---

## 2. HsnSoft.Base.AspNetCore.Mvc

### What it was
`ApiControllerBase` (`ControllerBase` with `IServiceProvider`-resolved Logger, Authorization, CurrentUser, CurrentTenant, StringLocalizerFactory), `BaseController` (MVC, with `RedirectSafely` and `Url.IsLocalUrl`), `RazorRenderService` (renders a page to a string through hand-built `RazorView`/`ViewContext`), and an anti-forgery set (`BaseAntiForgeryOptions`, `BaseValidateAntiforgeryTokenAuthorizationFilter`, cookie-name provider, manager that sets an `XSRF-TOKEN` cookie).

References: Security, AspNetCore, Ddd.Application, Localization, UI.Navigation, Razor RuntimeCompilation, and **`Microsoft.AspNetCore.Mvc.Versioning` 5.1.0**, a package deprecated years ago. Motto: broken (a controller library that pulled Ddd.Application, UI.Navigation and runtime Razor compilation).

Issues:
- `ServiceProvider` is a settable property that nothing sets, so `Logger` throws unless a derived class sets it.
- The anti-forgery cookie has a 10-year expiry and `SameSite=None`.
- `BaseValidateAntiforgeryTokenAuthorizationFilter` logs `LogError(exception.Message, exception)` (wrong overload).
- `RazorRenderService` constructs `RazorView` with `new DiagnosticListener(...)`, so a listener is allocated per render. It depends on `IActionContextAccessor`, which Microsoft says is slow and not registered by default.

### Where it is now
(b) `ApiControllerBase` is in `AspNetCore.WebApi` (ADR-0040: `Logger` and `CurrentUser` resolved lazily from the request services; no constructor). The rest is dropped (c). Anti-forgery is the built-in `AutoValidateAntiforgeryTokenAttribute`, as `samples/hsx/app/Hsx.CommercialWeb.Host/Program.cs` does. `RedirectSafely` is a few lines of `Url.IsLocalUrl` in the page.

### Why
ADR-0040 (controller base). The rest is **inferred**: the built-in anti-forgery covers it. hhs-analysis lists the Razor render service as "not carried" (0 uses; `AddMvcRazorRender` registered but never called).

### Verdict
Superseded (built-ins now do anti-forgery; the controller base was smaller and better in WebApi).

### Gap
None. Optional (Low): a short paragraph in `WebApi.md` or a new `WebUi.md` saying "for a cookie-auth Razor host use `AutoValidateAntiforgeryTokenAttribute`, `Url.IsLocalUrl`, and (if you want) the security-header middleware above". That is document-only.

### Keep and avoid
Keep: the `ShouldValidate` idea (always validate when the auth cookie is present; skip when no antiforgery cookie came, so non-browser clients are unaffected) is a good rule if a cookie-and-token mixed host ever appears, though the built-in `AutoValidate` already covers the common case. Avoid: the 10-year cookie, `SameSite=None` by default, and Mvc.Versioning 5.x. (Use `Asp.Versioning` only if a product needs API versions; the routes here use `v1` in the path, ADR-0026.)

---

## 3. HsnSoft.Base.AspNetCore.Mvc.UI

### What it was
Two files: `BasePageModel` (a `PageModel` with service-locator properties, `L` localizer, partial-view helper, `RedirectSafely`) and `TableModels.cs` (the DataTables request shape: `DataTablesRequest`, `Column`, `Order`, `Search`).

References: AutoMapper, NUglify, Mvc, UI.Navigation. Motto: broken (AutoMapper and NUglify were forced; NUglify is unused; the `IMapper` property contradicts ADR-0046).

### Where it is now
(c) Intentionally dropped. Nothing of it is in the new tree.

### Why
Inferred, with support. ADR-0046 says "DTO projection without a mapper". No hhs project references the package (`grep` for `Mvc.UI`, `BasePageModel`, `DataTablesRequest`: zero hits). The sample web hosts use plain Razor Pages.

### Verdict
Flawed and dead.

### Gap
None. The Hsx back office is an Angular/Razor client over the gateway, with `Paged<T>` DTOs from `Ddd.Application` (`PagedAndSortedRequestDto`).

### Keep and avoid
Nothing to keep. Avoid service-locator properties on a base page model and a mapper dependency in a UI package.

---

## 4. HsnSoft.Base.Swashbuckle

### What it was
`SwaggerConfigurationHelper.Configure/ConfigureWithBearer` (`AddEndpointsApiExplorer`, `AddBaseSwaggerGen[WithBearer]`, `SwaggerDoc`, `DocInclusionPredicate(true)`, `CustomSchemaIds(FullName)`, **plus** `services.AddOpenApi()`), `AddBaseSwaggerGen*` (maps `RemoteStreamContent` to binary, Bearer or OAuth2 security definition) and `UseBaseSwaggerUI`.

References: Swashbuckle.AspNetCore 10.2.3, `Microsoft.AspNetCore.OpenApi`, projects Mvc and VirtualFileSystem. Motto: over-coupled (a Swagger helper forced Mvc, Razor compilation and a virtual file system on a consumer).

Issues:
- `RemoteStreamContent` is an ABP type that no longer exists (dead mapping).
- Both Swashbuckle and the built-in OpenAPI were registered, which is two document generators.
- The `if (!IsHostProduction())` decision was repeated in every host Program.cs (hhs: text-normalizer, video-generator, content, identity, auth-server).
- The `UseBaseSwaggerUI` wrapper does nothing (its body is commented out).

### Where it is now
(a) Same capability, new name and shape: `HsnSoft.Base.AspNetCore.Swashbuckle` (`AddBaseSwagger`, `AddBaseSwaggerWithBearer`, `AddBaseSwaggerWithOAuth`, `UseBaseSwagger`, `SwaggerOptions`, `ISwaggerExtension`) and `AspNetCore.SwaggerGateway` (one UI for a gateway; `SwaggerDocumentRewriter`, which came from the hhs gateway's `Services/Swagger/SwaggerDocumentRewriter.cs`). It is off in `Production`, `Staging`, `stage` and `uat` by the code itself (`Swagger:Enabled`, `DisabledIn`, `RoutePrefix`).

### Why
Commits `a52e86ff` ("AspNetCore.Swashbuckle ... off in production-like environments; the OpenAPI document leaves WebApi") and `219a597f` (SwaggerGateway); ADR-0071; ADR-0119 fix `8962fcce` for the gateway documents. Docs: `docs/Swagger.md`.

### Verdict
Superseded by something better: the environment rule is in the package, there is one generator, Core-level dependencies only plus `AspNetCore`, and a settings section.

### Gap
Already covered. Two nits:
- `docs/Swagger.md` line 3 says "depends only on `HsnSoft.Base.Core`", while the csproj references `AspNetCore` (ADR-0071). Fix the sentence.
- hhs's `AddOpenApi()` (the built-in JSON document) has no replacement. Nobody consumed it; leave it dropped (None).

### Keep and avoid
Keep `CustomSchemaIds(FullName)` and the "all endpoints in the document" rule (already ported). Avoid the dead ABP type mapping and registering two generators.

---

## 5. HsnSoft.Base.Http.Client (special attention)

### What it was
(`dev-backup:src/HsnSoft.Base.Http.Client/...`) Three files:
- `BaseServiceAppClient`: an abstract base for typed clients. It takes `IHttpClientFactory`, offers `SetBearerToken(token)` (a `SetBearerToken` extension from `IdentityModel` called on the **shared** `HttpClient`), `GetQueryString(object)` (**reflection** over all properties, `ToString()` and `HttpUtility.UrlEncode`), `CheckResultAndReturnModel<T>`, `CheckResultAndReturnString`, and `CheckBaseResultAndReturnModel<T>` (unwraps `BaseResponse<T>`).
- `HttpClientAuthorizationDelegatingHandler`: copies the incoming request's `Authorization` header (and the `access_token` of the HttpContext, or the `access_token` claim) onto **every outgoing request, whatever the host**.
- `HttpClientRequestIdDelegatingHandler`: adds `x-requestid` (a new Guid v7) to POST and PUT.

References: **IdentityModel 7.0.0, `Microsoft.Extensions.Http`, and projects AspNetCore.Mvc, ExceptionHandling, MultiTenancy, Validation.** A typed HTTP client that dragged in MVC, Razor compilation, Validation and MultiTenancy. Motto: **broken** on "never force a library the consumer does not use".

Concrete bugs and risks:
- **Token relay to any host (security).** The authorization handler forwards the caller's bearer token to whatever URL the client calls. If a client is ever pointed at a third party (an AI provider, a CDN), the user's token leaks.
- Failures become `new Exception($"{status}:{messages}")` (a bare `Exception`), so a caller cannot tell 404 from 500 or retry. The model readers swallow all exceptions and return `null`, so a deserialization failure looks like "no data".
- It mixes Newtonsoft (`JsonConvert`) and System.Text.Json in one class.
- `SetBearerToken` on a client from the factory mutates a pooled/shared instance.
- No timeout, no retry, no circuit breaker, no cancellation tokens on any method.
- `x-requestid` is not an idempotency key anyone reads; it is just random.

### Where it is now
(d) **Missing.** Nothing named `Http.Client` exists. `docs/Overview.md` has no outgoing-HTTP package, and no ADR discusses it (commit `0c82c90e` removed it with everything else; **inferred**: it was never re-added because nobody asked).

What exists instead:
- `BaseResponse`/`BaseHttpException` are in `Core`.
- `LogFlow` (Core) already carries the correlation id of the current request or message as ambient `AsyncLocal` state, with a flow name constant `LogFlow.HttpClientName = "httpclient"`.
- .NET itself propagates W3C `traceparent` on every `HttpClient` call.
- The samples write plain typed clients plus a hand-written handler (below).

### How hhs really used it
Evidence by grep over `/Users/hasansahin/ws/org/tech-summus/hhs`:
- **The package's types are used nowhere.** Only `app/commercial/.../Hhs.Commercial.Web.Http.Host.csproj` references `HsnSoft.Base.Http.Client`. There is no `using HsnSoft.Base.Http`, no `BaseServiceAppClient`, and neither handler is registered. hhs ignored the package and used the plain factory.
- hhs outgoing calls are all raw `IHttpClientFactory` or `AddHttpClient()` with no handler, no resilience and no `Microsoft.Extensions.Http.Resilience` or Polly (grep of csproj and props: none):
  - Providers set `httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)` in their constructors (Outline fast/queue, Creatomate, Audio HQ/Quick/ElevenLabs, video queue internal/external).
  - `RemoteFileDownloader` (video-generator) streams a remote URL with `ResponseHeadersRead` into the CDN upload. It validates only that the URL is `http(s)`. It has **no block on private or loopback addresses (SSRF)**.
  - `ApiCallJob` (scheduler) calls endpoints with a `SchedulerApiKey` header and a self-made `job-<guid>` correlation header.
  - The commercial web host's `ClientTestLive` page fetches an arbitrary `clientUrl` with a 15-second timeout (also SSRF-shaped).
  - The gateway's `GatewayDocsProbeService` probes services with a 5-second timeout.
- **Resilience in hhs was a business-level retry, not an HTTP one.** Each provider catches the exception, classifies it with `ExceptionClassifier.IsRetryable(ex | statusCode)` (5xx, 429, `HttpRequestException`, `TimeoutException`, cancellation = retryable) and returns a DTO with `IsRetryable`. A work item then retries later with `RetryDelayCalculator`. This is a good split: a short in-call policy (a few fast attempts for a transient failure) and a long retry owned by the work item.
- **Correlation and headers in hhs:** `UseCorrelationId()` in commercial web and auth server only set the incoming header. Nothing propagated it on outgoing HTTP. The calls to the gateway from the commercial web host carried no token handler (the real handler was the unused package class).

### How the new samples use HTTP (the real current need)
`samples/{abc,hsx}/app/*Web.Host` each define the same `GatewayRequestHandler : DelegatingHandler` (identical apart from the namespace in Abc.CommercialWeb, Abc.PublicWeb and Hsx.CommercialWeb), registered by `AddHttpClient<GatewayClient>(...).AddHttpMessageHandler<GatewayRequestHandler>()`. It copies `X-Correlation-Id` from the current request and sets the `Bearer` token from the `access_token` claim in the cookie. `GatewayClient` then repeats `ReadFromJsonAsync<ApiResponse<T>>()` and `Payload` unwrapping in each method, with `ApiResponse<T>`/`Paged<T>` copied into the web host's `Models`. No timeout setting and no retry exist.

### Why
Inferred (no ADR): dropped with the rewrite start (`0c82c90e`), then nobody needed it until the sample web hosts appeared. ADR-0005 says move code into the framework when a second product wants it unchanged. Three copies in two samples of one shape is that signal.

### Verdict
The old design is **flawed**: wrong dependencies (MVC, Validation, MultiTenancy), a token relay to any host, a reflection query builder, swallowed errors and a mutated shared client. The *need* is real.

### Gap: add a package, priority Medium-High
Name: `HsnSoft.Base.Http.Client` (a new, different design). Capability: "named or typed `HttpClient` registrations for calling another service or API: base address, timeout and resilience from settings, correlation id and optional token propagation, and `BaseResponse` unwrapping with typed failures".

Right modern shape, measured against the motto:
- **References:** `Core` (for `BaseResponse`, `BaseHttpException`, `LogFlow`) + `Microsoft.Extensions.Http.Resilience`. **No** AspNetCore, MVC, Validation, MultiTenancy or IdentityModel. A worker or an agent can use it with no web stack.
- **Registration:** `services.AddBaseHttpClient<TClient>("Gateway", configuration)` reads `HttpClients:Gateway` (`BaseAddress`, `Timeout`, `Retry`, `CircuitBreaker`, `AllowedHosts`). It uses `AddStandardResilienceHandler` with the settings: a per-attempt timeout, a total timeout, a retry only for idempotent verbs or 408/429/5xx with jitter, and a circuit breaker. It has no hidden defaults that retry POST (ADR-0119: "retry only what is safe to retry").
- **Correlation handler:** `CorrelationDelegatingHandler` reads `LogFlow.Current?.CorrelationId` (works in a request, a consumer, a scheduler job, with no `HttpContext`) and sets `X-Correlation-Id`. The header name comes from `RequestLogging:CorrelationIdHeader`. `traceparent` is already automatic.
- **Token propagation is a separate, explicit, opt-in piece** that lives where `HttpContext` exists (in `AspNetCore`, or a small `Http.Client.AspNetCore` layer per ADR-0003/0071). It relays the caller's token **only to hosts in `AllowedHosts`** (that is the fix for the old leak). A cookie-auth web host takes the `access_token` claim instead (what `GatewayRequestHandler` does today).
- **Response handling:** `ReadBaseResponseAsync<T>(CancellationToken)` returns `payload` and, on failure, throws `BaseHttpException(status, messages, errorCode)` so a caller can distinguish 401, 404, 429 and 5xx. It uses System.Text.Json only, with no reflection query builder (a caller builds the query with `QueryHelpers` or an interpolated string).
- **A safe outbound policy for user-supplied URLs (SSRF):** an optional `SsrfGuardHandler` that refuses loopback, link-local and private ranges after DNS resolution (check the resolved IP in `ConnectCallback`, not only the string). It protects `RemoteFileDownloader`-style code (hhs video-generator) and `ClientTestLive`-style code.
- **Who would use it:** Hsx.CommercialWeb.Host (replaces `GatewayRequestHandler`, `GatewayClient`'s unwrapping), Abc web hosts, Hsx text-normalizer and video-generator providers (timeouts and the in-call resilience; business retry stays in the work item), the Hsx scheduler and any agent.
- **Keep the split of retries:** in-call resilience belongs in the package; the long retry with `NextAttemptAt` belongs to the work-item worker proposed in `hhs-analysis.md` section 5.

### Keep and avoid
Keep:
- The idea of `CheckResult` (read the `BaseResponse` of a failed call into one error carrying the server's messages). Port it as a typed exception, not `new Exception(string)`.
- The `x-requestid` idea only if an endpoint really reads it for idempotency (none does; skip it until one does).
- hhs's `ExceptionClassifier.IsRetryable(statusCode | exception)` (5xx and 429 retryable, other 4xx permanent; `HttpRequestException`, `TimeoutException`, `OperationCanceledException`, `IOException` retryable) as the default classification of the resilience handler, and as part of the work-item building block. Source: `hhs/shared/src/Hhs.Shared.Helper/Retry/ExceptionClassifier.cs`.

Avoid:
- Forwarding `Authorization` to any host.
- `SetBearerToken` on a shared client.
- Swallowing deserialization errors into `null`.
- A reflection query-string builder.
- Mixed JSON libraries.
- A bare `Exception`.
- Referencing MVC, Validation and MultiTenancy from an HTTP client library.

---

## 6. HsnSoft.Base.UI

### What it was
`IBrandingProvider`/`DefaultBrandingProvider` (AppName, logo URLs, an ABP leftover) and `BaseUiResource` (localization resource named `UiResources`, with `en/de/ru/tr.json`). Reference: ExceptionHandling only. Motto: fine in size, but it was a pure ABP clone with no purpose of its own.

### Where it is now
(c) Dropped. Branding is app data in a product. Localization is the `Localization` package (embedded JSON, ADR-0038).

### Why
Inferred. No hhs file references `HsnSoft.Base.UI` (grep: zero). ADR-0038 replaced the resource-type model.

### Verdict
Dead code (no consumers).

### Gap
None.

### Keep and avoid
Nothing to keep.

---

## 7. HsnSoft.Base.UI.Navigation

### What it was
An ABP-style menu model: `ApplicationMenu`, `ApplicationMenuItem` (Name, DisplayName, Url, Icon, Order, `RequiredPermissionName`, `IsGranted`), `ApplicationMenuItemList`, `IMenuContributor`, `DefaultMenuContributor` (adds one "Administration" item), `MenuConfigurationContext` (service-locator access to the localizer; the authorization calls are commented out), `StandardMenus` (`Main`, `User`, `Shortcut`) and four language json files. References: Authorization, UI.

### Where it is now
(c) Dropped. In Hsx the menu is **data** in the identity service: samples ADR-0062 ("the permissions and the menu of hsx are the codes and live in the identity service"). hhs-analysis says hhs's menu was "a 328-line static class" with unused menu tables.

### Why
Samples ADR-0062 and `samples/hsx/docs/hhs-analysis.md` (section 4: "the menu is data"; "Not carried: `OldMenuMap`, `AppMenu` tables nobody queries"). The framework's `Authorization` package already has page permissions (`[PermissionAuthorize]`, page, endpoint, operation, constraint).

### Verdict
Dead code (no consumer in hhs, `IsGranted` never computed). The data-driven approach is better.

### Gap
None (**keep dropped**). A menu is a product concept, so it fails ADR-0005 test 1. If a second product wants a menu, the shape worth porting is "permission-filtered tree, localized by key", but not the ABP classes.

### Keep and avoid
Keep only the idea `RequiredPermissionName` per item, filtered by the permission store (already how Hsx page permissions work). Avoid a menu contributor model with a service locator.

---

## 8. HsnSoft.Base.PuppeTeer (special attention)

### What it was
(`dev-backup:src/HsnSoft.Base.PuppeTeer/...`) A managed headless-Chrome host over PuppeteerSharp 25.3.3 (reference: Core only; good).
- `IPuppeteerBrowser`/`PuppeteerBrowser`: one shared browser per process, eager init at construction, launch with retry and backoff (`BrowserLaunchRetryCount`, `BrowserLaunchRetryDelaySeconds`), a page **limiter** (`SemaphoreSlim(PageMaxCount)` with `WaitFreePageTimeoutMs`), `AcquirePageAsync` returning a `PuppeteerPageLease` (an `IAsyncDisposable` that closes the page and releases the slot), a lifecycle gate, reconnect after `Disconnected`, graceful drain on shutdown (`RequestShutdownAndDrainAsync`), proxy from `PUPPETEER_PROXY_HOST/PORT`.
- `PuppeteerPdfRenderer.RenderPdfFromHtmlAsync(html, PdfOptions)`: set content, wait for DOM and fonts, `PdfDataAsync`.
- `PuppeteerShutdownHostedService`, `PuppeteerBrowserSettings`.

It is the best-written project in this cluster (leases, bounded concurrency, drain, no static state except below).

Problems:
- **Unsafe defaults:** `--no-sandbox`, `--disable-setuid-sandbox`, `--ignore-certificate-errors`, `--allow-insecure-localhost`, `--disable-blink-features=AutomationControlled`. Chrome without a sandbox renders hostile pages with no isolation, and disabling certificate checks makes every scraped page spoofable. Also, a non-empty `Args` setting **replaces** the whole default list instead of adding to it.
- It downloads Chromium at runtime with `BrowserFetcher` when not in a container (supply chain and startup latency; air-gapped hosts fail).
- It sets a **global static** `TaskHelper.DefaultTimeout = 30 * 60 * 1000` (30 minutes, for the whole process) in the constructor.
- Eager launch in the constructor (`_ = EnsureBrowserInitializedCoreAsync(...)`) with no await, so a failure is hidden in `InitializationResult` (a string).
- `Logger` is `IAppConsoleLogger` (the old Serilog-based logger), so it tied the tool to the logging package.
- `PuppeteerPdfRenderer` renders caller-supplied HTML with network access, which is an SSRF and local-file-read vector if the HTML is user-controlled (no request interception).
- Hard-coded waits in the renderer (30 s, 5 s, 5 s).

### Where it is now
(d) **Missing.** Not in `src/`, not in `docs/Overview.md`, no ADR. `samples/hsx/docs/hhs-analysis.md` section 3 records the decision: "Scraping (Puppeteer...): none (the old framework had a `PuppeTeer` package) = **Product**", and section 4: "the Puppeteer wrapper (text-normalizer only)" moves into the one service that uses it. Hsx text-normalizer does not yet scrape (the `grep` shows only DTO and domain fields).

### How hhs really used it
- **One service only:** `service/text-normalizer`. `IPuppeteerBrowser` is a singleton (`ApplicationServiceCollectionExtensions.cs`), and `PuppeteerContentScraper` (scoped) picks a scraper by site: Cnbce, Diyetkolik, InStyle, Mynet, Ntv, T24, Tamindir, TechnoToday and a demo class, all deriving from `BaseScrapingService` (`Providers/Scraping/`).
- **Only scraping, not PDF and not screenshots.** `PuppeteerPdfRenderer` is not used anywhere (`grep PdfData|Screenshot|PdfRenderer` in hhs: zero hits). So the PDF capability had **no consumer**.
- The use is: acquire a lease with a per-page setup callback (proxy auth, a fixed desktop Chrome user agent, `Accept-Language: tr-TR`, cache off, a stealth script, request interception that aborts media and optionally fonts, CSS and scripts), `GoToAsync(url, DOMContentLoaded, 60 s)`, then evaluate five selector scripts (title, release time, spot, details, image). Failures are classified retryable or not (`ExceptionClassifier`) and the work item retries.
- The host runs a startup probe (opens a page, logs "Puppeteer unavailable, running in degraded mode" if it fails), and `PuppeteerShutdownHostedService` plus `HostOptions.ShutdownTimeout = ShutdownDrainTimeoutSeconds` for the drain.
- The appsettings repeat the unsafe args (`--no-sandbox`, `--ignore-certificate-errors`, ...).
- A proxy is configured through `PUPPETEER_PROXY_HOST/PORT`, and the scraper authenticates it per page.

### Why
Hsx analysis (above) and ADR-0005 (a capability only one service uses stays in the service). Before that, the rewrite start (`0c82c90e`) dropped it. **Inferred** that nobody has re-decided it since.

### Verdict
Sound core idea (lease, limiter, drain, reconnect), unsafe defaults, unneeded PDF half. Superseded by nothing, so the question is whether it deserves a package.

### Gap: add an opt-in package, priority Medium (below Http.Client)
Reason for a package: a headless browser host is **infrastructure** (lifecycle, concurrency bound, shutdown drain, health) with no product knowledge, and the part hhs got right is hard to get right (a leaked page or a zombie Chrome per request is a classic outage). The motto fits: it would be a separate opt-in package that only a scraper, PDF or screenshot service references, so nothing else loads PuppeteerSharp. But the Hsx analysis says Hsx writes the scraping as data-driven rules in its text-normalizer, and ADR-0005 (one service uses it) argues for keeping it in that service until a second service or product needs it. Recommendation: **build it as `HsnSoft.Base.Browser` (or keep the name `PuppeTeer` fixed to `Puppeteer`) when the Hsx text-normalizer scraper is implemented, if the maintainer wants the lifecycle as a package**. Otherwise port the class into the service.

If built:
- **Name and capability:** `HsnSoft.Base.Browser.Puppeteer`: "a pool of headless Chromium pages with leases: render HTML or a URL to PDF, screenshot or DOM, with bounded concurrency and a clean shutdown". Abstraction `HsnSoft.Base.Browser` (`IBrowserPageLease`, `IPdfRenderer`, `IScreenshotService`) and the provider package follow ADR-0003, so a Playwright provider could replace it.
- **References:** Core + PuppeteerSharp (provider). Logging through `ILogger` (not `IAppConsoleLogger`), health check `browser` through `AddBaseHealthChecks` (ADR-0117, tag `ready`, disabled by name), settings in a `Browser` section.
- **Secure by default:** the sandbox on (document the `--no-sandbox` need in containers as an explicit opt-in setting with a warning, or use a non-root user with `--disable-dev-shm-usage`); certificate checks on; user args **added** to a safe base list, never replacing it; **no** runtime download in production (`ExecutablePath` from the image; download only in Development); `TaskHelper.DefaultTimeout` not set globally (set a per-page timeout); a mandatory request interceptor with an allow-list when rendering untrusted HTML (block `file://`, private ranges and metadata addresses; same SSRF guard as `Http.Client`).
- **Behaviour to port:** `PuppeteerPageLease` and the page limiter (`PuppeteerBrowser.AcquirePageAsync`), launch retry with growing delay, `Disconnected` recovery, drain on stop, and the hosted-service stop order. The Hsx scraping rules (selectors as data) stay in the service.
- **Who uses it:** Hsx text-normalizer (scraping), later any service that renders a PDF (an invoice or a report: Edx invoice pipeline "sign" step is a candidate) or takes a screenshot (video-generator thumbnails).

### Keep and avoid
Keep: `PuppeteerPageLease` (an `IAsyncDisposable` that is idempotent through `Interlocked.Exchange`), the `SemaphoreSlim` limiter with timeout, the lifecycle gate plus `Disconnected` reconnect, the drain `TaskCompletionSource`, the startup availability probe that degrades instead of failing (hhs text-normalizer `Program.cs`), the request-interception idea of dropping media and fonts for speed, and the wait for `document.fonts` before a PDF.
Avoid: all of the unsafe args as defaults, the global `TaskHelper.DefaultTimeout`, the eager fire-and-forget init in a constructor, `InitializationResult` as a status string, stealth tricks in the library (product rule), replacing the default args wholesale, and runtime browser download.

---

## 9. HsnSoft.Base.Serilog

### What it was
(`dev-backup:src/HsnSoft.Base.Serilog/...`) The logging library of the old framework:
- `Loggers/PersistentLogger` (abstract; each of `AppLogger`, `FrameworkLogger`, plus `RequestResponseLogger` and `EventBusLogger` in other projects, built **its own** Serilog pipeline: console plus Graylog or file), `RefreshSink()` (rebuilds the pipeline every N minutes because the Graylog sink can stay broken after a failed start).
- `SerilogConfigurationHelper` (console template, Graylog GELF sink, file sink, a table of minimum-level overrides by namespace), `LogMasker`/`Mask/*` (`MaskingHelper` by key name, `JsonDataMasking`, `SensitiveDataAttribute`, `SensitiveDataDestructuringPolicy`).

References: Core + Serilog, Serilog.Extensions.Logging, Sinks.Async/File/Console/Graylog, Destructurama.Attributed and JsonNet, Compact formatter and DeepCloner. Motto: **over-coupled**. Product code that wanted the `IAppConsoleLogger` abstraction got Serilog, Graylog and Newtonsoft destructuring. Consumers could not swap the provider.

Problems:
- Each logger type created a separate pipeline and Graylog connection (three or four per process), configured by the section `FrameworkLogger:*`.
- Static `Log.Logger` and `AppDomain.CurrentDomain.FriendlyName` as the `Solution`/`Assembly` (breaks when the host is renamed or tested).
- `Destructure.JsonNetTypes()` forced Newtonsoft.
- The key-name list in `MaskingHelper` includes `username` but only matches whole key names (a `user_password` key is not masked).
- `LogMasker.MaskObject` swallows every exception and returns the unmasked `value` in the last fallback (**a failed mask leaks the value**).
- `Logger.RefreshSink` disposes the old pipeline immediately, losing events still in flight (the new code keeps the replaced sink alive for ten seconds; commit `29f52c2f`).

### Where it is now
(b) Split in three, all present and bigger:
- `Logging` (independent of any library): `IBaseLogger`, `AppConsoleLogger`, `LogShape`, `LogRouting`, `LogLevelRules`, `PersistentLevelRules`, `Masking/*` (`LogMasker`, `JsonDataMasking`, `MaskingHelper`, `SensitiveDataRegistry`, `SensitiveDataTree`, `SensitiveDataShape`).
- `Logging.Serilog`: `BaseSerilog` (`AddBaseSerilog`), `SerilogSinks`, `SerilogRouting`, `RefreshableSink` + `GraylogSinkRefreshService` (the self-healing sink, with a graceful hand-over), `FieldGuardSink`, `FacilityEnricher`, `LogShapeEnricher`, `NoiseEnricher`, `SensitiveDataDestructuringPolicy`.
- `Logging.NLog` as a second provider with the same settings (ADR-0076).
All framework packages write through `ILogger<T>` (ADR-0015); Newtonsoft destructuring is now the product's option (`serilog => serilog.Destructure.JsonNetTypes()`). Docs: `docs/Logging.md`, `LogLevels.md`, `GraylogFields.md`.

### Why
ADR-0015 (everything uses `ILogger`), 0016 (log types by namespace), 0017 (two places, two levels), 0018 (chain logs), 0019 (fields pipeline), 0020 (a failure is logged once), 0021 (sensitive data by attributes), 0076 (more providers and the rules every provider shares); commit `29f52c2f` (the replaced Graylog sink is flushed on stop).

### Verdict
Sound in what it did (Graylog, masking, refresh) but over-coupled and static-heavy. **Superseded by something better**: one provider-independent `Logging` package, per-library provider packages, one pipeline per process, and the self-healing sink kept.

### Gap
Covered. One item to verify (not a gap): confirm the new `LogMasker` fallback does not return the original value when masking fails (the old code did); I did not audit the new masker line by line, so treat this as a check for the logging cluster's owner.

### Keep and avoid
Keep: the self-healing Graylog sink idea (kept), `SensitiveDataAttribute`, `IsSelfLogActive` (kept). Avoid: a pipeline per logger kind, a catch-all that returns the unmasked value, `AppDomain.FriendlyName` as the product identity, and forced Newtonsoft destructuring.

---

## 10. Health checks, middleware, startup and hosting (cross-cutting comparison)

### Old own health checks (`dev-backup`)
- `Caching.StackExchangeRedis/RedisHealthCheck.cs` (`CustomRedisHealthCheck`): two constructors. With a connection **string** it opens a new `ConnectionMultiplexer` on **every probe** and disposes it on failure; with a multiplexer it pings each standalone endpoint. It reports "Redis is ready" even when **no standalone server** is found (cluster or sentinel endpoints count as healthy without a ping). Exceptions in the per-endpoint loop are swallowed.
- `EventBus.RabbitMQ/RabbitMqHealthCheck.cs`: builds a `ConnectionFactory` and **opens a new connection on every probe**, with `RequestedHeartbeat` 60 s. A busy cluster with a probe every 5 s means one extra TCP+AMQP connection per probe per pod.
- hhs `AddHostingHealthChecks` used the third-party `AspNetCore.HealthChecks.*` packages for Mongo and Postgres, three endpoints `/StartupCheck`, `/LivenessCheck`, `/ReadinessCheck`, the liveness endpoint filtered by the name `self-check`, and a response writer that returned `exception.Message` of every check (addresses leaked).

### New
`AspNetCore` provides `/health/live`, `/health/startup`, `/health/ready` (`Health.md`, ADR-0117), a per-check timeout, `Health:Disabled` by name, and `Health:IncludeErrors=false` by default (the message is not shown). Each package adds its own check: `redis` (a ping on the shared connection), `rabbitmq` (the **service's own** connection is open; no probe connection, ADR-0127), `kafka` (metadata request), `mongo`, `postgres/sqlserver/mysql`, `minio/azure-blob/aws-s3`. `AddBaseReadyCheck<T>` for a service's own check. Verdict: old checks flawed (connection per probe, false healthy), new ones better and consistent.

### hhs shared hosting vs new

| `Hhs.Shared.Hosting*` piece | New equivalent | Note |
|---|---|---|
| `AddCommonAspNetCoreHosting`, `ForwardedHeaders` | `AddBaseHosting`, `UseBaseForwardedHeaders` (known proxies) | hhs trusted every proxy header (no `KnownProxies`): spoofable client IP. Fixed |
| `UseCustomExceptionHandler` (status-code pages with `http_404` and binary-content exemptions) | `UseBaseExceptionHandling` (WebApi.md: `http_404`, not for files) | Same rule |
| `UseHostingHealthChecks` | `MapBaseHealth` | Better |
| `AddJwtServerAuthentication` (RSA `public_key.xml`) | `Authentication` (ADR-0122, `Key` or `PublicKeyFile`) | The new one validates issuer outside Development |
| `ThreadPoolConfigurationHelper`, `AppIdResolver` | `Hosting:ThreadPool`, `AppInfo` (ADR-0121) | Covered |
| `AppBasicLoader` + `LoaderHostedService` | startup steps and seeders (ADR-0118, 0125) | Covered |
| `ClientApiKeyAuthAttribute` | `[SchedulerAuth]` (ADR-0126), key required at start | The old one returned a 400 when the header was missing (should be 401) |
| `EndpointConcurrencyLimitAttribute` (Redis counter, TTL) | none | Hsx analysis: not needed with claims by compare-and-set |
| `ApiRequestResponseLoggingMiddleware` | `RequestLoggingMiddleware` | Covered |
| `SearchEngineAgentMiddleware` (`/robots.txt`: `Disallow: /` unless a file exists) | none | Document only or a product line; trivially `app.MapGet("/robots.txt", ...)`. Priority None |
| `UserAgentProvider` (UAParser; used by the public content controller for visit analytics) | none | Product (Hsx public gateway visit log); needs a third-party parser, keep out of the framework |
| `AddDefaultCorsSettings` (any origin + credentials) | none (Edx has a product policy) | See section 1 gap |
| `AddHostingRedis` (`IRedisRepository`, `RedisLockService` unused, `IRequestLimitStore`) | `Caching.StackExchangeRedis` (`AddBaseRedis`) | Redis lock unused in hhs |
| `UserTenantCheckerMiddleware` (Microservices) | `MultiTenancy.Authentication` | Covered |

The new composition model (`Add{Product}Hosting` in the product's shared project, see `samples/hsx/shared/Hsx.Shared.Hosting/HostingRegister.cs`) matches ADR-0005 exactly.

---

## Open questions for the maintainer

1. **Http.Client:** build the new `HsnSoft.Base.Http.Client` now (Core + `Microsoft.Extensions.Http.Resilience`, correlation from `LogFlow`, token relay as a separate opt-in layer limited to `AllowedHosts`), or keep the handler as sample code until a second product asks (ADR-0005)? The count is already three copies in two samples.
2. **Outbound SSRF guard:** should the framework ship a guard handler for user-supplied URLs (hhs's `RemoteFileDownloader` and `ClientTestLive` are the cases)? It is a security feature, so I recommend yes, inside `Http.Client`.
3. **PuppeTeer:** a framework package (`Browser` abstraction + `Browser.Puppeteer`) or a class inside the Hsx text-normalizer service? Do you want PDF and screenshot to be in scope, or scraping only (the only real hhs use)?
4. **Security headers:** a `UseBaseSecurityHeaders()` in `AspNetCore` (HSTS, nosniff, frame, referrer policy, optional CSP), or leave it to the product / reverse proxy?
5. **CORS:** a `Hosting:Cors` section (explicit origins, no wildcard with credentials) in `AspNetCore`, or keep it per-product as in Edx?
6. **Docs fixes:** correct the `Swagger.md` sentence about the Core-only dependency of Swashbuckle (it references `AspNetCore`, ADR-0071). Should I add a short "web UI host" recipe (anti-forgery, safe redirect, headers)?
7. **Work-item worker:** the "durable work item with retry" block (hhs-analysis section 5) decides how `ExceptionClassifier` and the in-call resilience of `Http.Client` fit together. Do you want that decided before `Http.Client` is built?
