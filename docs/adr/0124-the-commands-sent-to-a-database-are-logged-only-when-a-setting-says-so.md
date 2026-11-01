# ADR-0124: The commands sent to a database are logged only when a setting says so

- **Status:** Accepted
- **Date:** 2026-11-01 (adds to [ADR-0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md))

## Context

EF Core logs every command it runs at Information, under the category `Microsoft.EntityFrameworkCore.Database.Command`, so a busy service wrote a line for every query unless somebody found the right line of `Logging:LogLevel` (and the console and the
persistent logger have levels of their own). The MongoDB driver logged nothing, so what a service asked of its database could not be seen at all. The old system had one switch for the values of the parameters (`EnableSensitiveDataLogging`, on outside
production) and nothing for the commands themselves. A developer wants the commands in development and a service in production does not.

## Decision

- **`EntityFramework:LogCommands`** (default false) and **`Mongo:LogCommands`** (default false) say whether the commands that run are logged. For EF Core, when it is off the context ignores the event of an executed command (`ConfigureWarnings(... Ignore(RelationalEventId.CommandExecuted))`), so EF Core does not raise it and no
  provider of logs can show it, and **a command that fails is another event (an Error) and is logged either way**. For MongoDB the framework logs one line per command from the events of the driver: the command, the database
  and collection and how long it took (a failed one is a Warning with the error); the driver's own commands (the handshake, the heartbeat) are left out.
- **What is in a command is never logged:** not the values of EF's parameters (sensitive-data logging stays off) and not the document of a MongoDB command. They are data.
- A host without logging has no log of the Mongo commands (the option needs an `ILoggerFactory`).

A first version set the level of the category with a logging filter. It held in a unit test and did **not** hold in a pod: the Serilog provider of the framework has a rule of its own that opens its level wide, and a rule of a
provider beats a rule of a category. The event is therefore switched off where it is raised, and the test runs real commands with such a provider rule present.

## Consequences

- A service has the commands in the log of development and none in production, by one setting each, with the failures always visible.
- The log of the commands of MongoDB, which did not exist, is there when wanted.
- A service that relied on the Information lines of EF Core in production sets `EntityFramework:LogCommands`.

## Related

[ADR-0119](0119-the-infrastructure-clients-share-their-connections-and-bound-their-waits.md), [ADR-0021](0021-sensitive-data-by-attributes.md).
