# ADR-0164: The file log is on by default in the Development environment only, and `IsFileLogActive` turns it on or off

- **Status:** Accepted
- **Date:** 2026-11-19 (changes the part of ADR-0017 that said what is kept goes to the file when `IsGrayLogActive` is `false`)

## Context

ADR-0017 made the persistent place Graylog or, when `IsGrayLogActive` is `false`, the file `PersistentLogger:FilePath`. Running the services in docker and on Kubernetes showed what that does where the
file cannot be written: every container logged hundreds of `Serilog SelfLog: the target file could not be opened or created` (a read only root file system, a user with no home for the named mutex of the shared file),
and a file inside a container is lost with the container anyway. UAT and production use Graylog and never write the file; the file is useful on the machine of a developer who runs the service from the IDE.

## Decision

- What is kept goes to **Graylog** when `PersistentLogger:IsGrayLogActive` is `true`. Graylog or the file, never both; the console is always on.
- When Graylog is off, what is kept goes to the **file** `FilePath` when **`PersistentLogger:IsFileLogActive`** is `true` (`PersistentLoggerOptions.WritesFile`: Graylog off and the file active). With both off nothing is kept and the console is the
  only place; no file is made.
- **The default of `IsFileLogActive`** (when the setting is not given, or is not `true` or `false`) is the environment: on in Development, off in any other (docker, k8s, test, UAT, production). The environment is read from the key `environment` (what the host
  builder fills from `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`) or from those two names; it is Development when the name is `Development` (case does not matter, spaces are trimmed); without a name it is not Development.
  A given setting wins over the default, so an environment that needs the file asks for it (`PersistentLogger__IsFileLogActive=true`) and a developer who does not want it turns it off.
- Both providers do the same: `HsnSoft.Base.Logging.Serilog` (a sink that keeps nothing) and `HsnSoft.Base.Logging.NLog` (a `NullTarget`).

## Consequences

- A service that relied on the file outside Development (a container that wrote `Logs/...`) writes nothing there any more unless `IsFileLogActive` is set; it needs Graylog (`IsGrayLogActive: true`) to keep logs. The self-log errors of the file sink in containers are gone.
- A host that is not Development, has Graylog off and does not ask for the file keeps nothing: the console (`docker logs`, `kubectl logs`) is the log.
- The services take it with the next version of the framework; no service file changes. A container that asks for the file must also make the path writable (a read only root file system needs a mount at the folder of `FilePath`).

## Related

- [ADR-0017](0017-two-places-two-levels.md) (the two places and their levels), [Logging.md](../Logging.md), [LogLevels.md](../LogLevels.md).
- `PersistentLoggerOptions`, `SerilogSinks`, `BaseNLog`.
