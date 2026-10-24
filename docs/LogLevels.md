# Log levels: where a log can be seen

A log goes to **two places**, each with its own level: the **console** (the pod's log, `kubectl logs`) and what is **kept** (Graylog, or a file). Changing a level
changes one place, not the other. This page says what each setting changes and where a log of each level can be seen. The settings are explained in
[Logging.md](Logging.md); what fields each log type has is in [GraylogFields.md](GraylogFields.md).

## The settings, and the place each one decides

| Place | Setting | Default | What it says |
|---|---|---|---|
| **Console** | `Logging:LogLevel:Default` | `Information` | the lowest level shown for every category that has no rule of its own |
| **Console** | `Logging:LogLevel:<Namespace>` | none | the lowest level shown for that namespace and everything below it (`"Microsoft.AspNetCore": "Warning"`, `None` for silence) |
| **Graylog or file** | `PersistentLogger:LogLevel` | `Information` | the lowest level kept, for every category that has no override. **Never below Information** |
| **Graylog or file** | `PersistentLogger:LevelOverrides:<Namespace>` | none | the lowest level kept for that namespace (and below it), instead of `LogLevel`. Never below Information |
| **Graylog or file** | `PersistentLogger:PlatformMinimumLevel` | `Warning` | the lowest level kept for the platform's logs (ASP.NET Core, drivers, any library) whose namespace no override names |
| **Which one** | `PersistentLogger:IsGrayLogActive` | `false` | `true`: what is kept goes to **Graylog**; `false`: to the **file** `PersistentLogger:FilePath`. Never both |
| *(neither)* | `PersistentLogger:IsSelfLogActive` | `false` | the logging library's own errors (Graylog cannot be reached, a sink broke) are written to the error output. It does not change which logs are written |

A namespace rule counts for the namespace and everything below it, the longest rule that fits wins, and the name of the category is the class that wrote the log.

## For one log: how it is decided

1. **Console:** the level of the log is compared with the rule of its category in `Logging:LogLevel` (else `Default`). At or above it: shown.
2. **Kept:** a log of `ConsoleOnly.*` (`IAppConsoleLogger`) is never kept. Otherwise its level must reach
   - the override of its category in `PersistentLogger:LevelOverrides`, if one names it; else `PersistentLogger:LogLevel`;
   - for a **platform** log that no override names, at least `PlatformMinimumLevel`;
   - and never less than **Information**.

The two steps do not look at each other: a log can be in the console and not kept, kept and not in the console, both, or neither.

## Where a log of each level can be seen: the default settings

`Logging:LogLevel:Default` = Information, `PersistentLogger:LogLevel` = Information, `PlatformMinimumLevel` = Warning, Graylog on.

| Level | Console | Graylog: application, shared library, framework, request and message logs | Graylog: platform logs |
|---|---|---|---|
| Trace / Debug | no (below the default) | **never** | **never** |
| Information | yes | yes | no |
| Warning | yes | yes | yes |
| Error | yes | yes | yes |
| Critical (Serilog: Fatal) | yes | yes | yes |

The types (see [Logging.md](Logging.md)): `AppLog` is written by the classes under the application's namespace, `SharedLog` by `{AppNamespace}.Shared.*`, `FrameworkLog` by `HsnSoft.Base.*`, the request
and message logs (`HttpRequest`, `HttpResponse`, `HttpRequestResponse`, `ConsumeStart`, `ConsumeEnd`) by the framework, and `PlatformLog` by everything else.

## What changing a setting does

| I change | Console | Graylog / file |
|---|---|---|
| `Logging:LogLevel:Default` to `Warning` | only warnings and above, from every namespace without a rule | nothing changes |
| `Logging:LogLevel:Hhs` to `Debug` | the Debug logs of the application appear | nothing changes: **Debug is never kept** |
| `Logging:LogLevel:Microsoft.AspNetCore` to `Warning` | the `Request starting` / `Executing endpoint` lines of ASP.NET Core go away | nothing changes (the platform's Information was not kept anyway) |
| `Logging:LogLevel:Chatty` to `None` | that namespace is silent | nothing changes |
| `Logging:LogLevel:HsnSoft.Base` to `Warning` | the request and message logs (Information) and the framework's Information disappear | nothing changes |
| `PersistentLogger:LogLevel` to `Warning` | nothing changes | only Warning and above are kept: the `HttpRequest`, the successful `HttpResponse` (Information) and `ConsumeStart` are no longer kept; the 4xx (Warning) and 5xx (Error) responses and a failed message still are |
| `PersistentLogger:LogLevel` to `Debug` or `Trace` | nothing changes | nothing changes: it is read as Information, the floor |
| `PersistentLogger:LevelOverrides:Hhs.Orders.Noisy` to `Warning` | nothing changes | only the warnings of that namespace are kept, the other namespaces keep `LogLevel` |
| `PersistentLogger:LevelOverrides:MongoDB` to `Information` | nothing changes (set `Logging:LogLevel:MongoDB` to see them too) | the driver's Information is kept, as a named platform namespace is not held to `PlatformMinimumLevel` |
| `PersistentLogger:PlatformMinimumLevel` to `Error` | nothing changes | the platform's warnings are no longer kept, only its Error and Critical |
| `PersistentLogger:PlatformMinimumLevel` to `Information` | nothing changes | the platform's Information is kept too (a lot of it) |
| `PersistentLogger:IsGrayLogActive` to `false` | nothing changes | nothing goes to Graylog; it is written to `FilePath` |
| `PersistentLogger:IsSelfLogActive` to `true` | nothing changes | nothing changes; if Graylog cannot be reached the error is written to the error output |

## Settings for the usual cases

**A quiet pod, Graylog complete** (production)

```json
"Logging": { "LogLevel": { "Default": "Warning" } },
"PersistentLogger": { "LogLevel": "Information", "IsGrayLogActive": true }
```
`kubectl logs` shows warnings and errors only; Graylog keeps the Information of the application, the requests and the messages.

**Look at the Debug of one namespace in the console** (a developer, or a pod that is being investigated)

```json
"Logging": { "LogLevel": { "Default": "Information", "Hhs.Orders": "Debug" } }
```
The Debug logs of `Hhs.Orders` are in the console only; Graylog is not touched.

**Keep only the problems in Graylog** (to keep what it stores small)

```json
"PersistentLogger": { "LogLevel": "Warning", "IsGrayLogActive": true }
```
Failed requests and messages are kept; the successful request and message logs are not, and the console is not touched.

**See a library's Information in Graylog** (the MongoDB driver)

```json
"Logging": { "LogLevel": { "Default": "Warning", "MongoDB": "Information" } },
"PersistentLogger": { "IsGrayLogActive": true, "LevelOverrides": { "MongoDB": "Information" } }
```

**Mute a chatty namespace of your own in Graylog, not in the console**

```json
"PersistentLogger": { "LevelOverrides": { "Hhs.Orders.Polling": "Warning" } }
```

**No Graylog (a developer's machine, or Graylog is not there yet)**

```json
"PersistentLogger": { "IsGrayLogActive": false, "FilePath": "Logs/orders.txt" }
```
What would go to Graylog is written to the file, by the same levels.

## Where the settings live

They are ordinary .NET configuration, so every source works: `appsettings.json`, `appsettings.{Environment}.json`, and environment variables with `__` for `:`
(`Logging__LogLevel__Default=Warning`, `PersistentLogger__LogLevel=Warning`, `PersistentLogger__LevelOverrides__MongoDB=Information`, `PersistentLogger__IsGrayLogActive=true`). In Kubernetes the environment
variables of the deployment are the way to change a level of a running service without a new image. A change is read when the service starts.

## What does not change with any setting

- Debug and Trace are **never kept**, whatever the level of a namespace or of `PersistentLogger:LogLevel` says. They can be in the console.
- A log of `ConsoleOnly.*` (`IAppConsoleLogger`) is never kept. Its level in the console follows `Logging:LogLevel` like any other (add `"ConsoleOnly": "Debug"` to see its Debug).
- The section read for the console is only `Logging:LogLevel`: provider sections such as `Logging:Console:LogLevel` do not apply.
- A sink that a product adds itself (the `configure` of `AddBaseSerilog`) is not held to these levels: it gets every log.
- Every log is created whatever the levels say (a Debug log is built, then dropped by both places); the cost is small, but a Debug log in a very hot loop is not free.
