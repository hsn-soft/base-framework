# Preparing a database at startup

Who creates a database, its schema and its seed data when a host starts, and how pods and hosts that start together do not meet ([ADR-0118](adr/0118-a-database-is-prepared-by-the-host-that-owns-it-one-pod-at-a-time.md)).

## The owner

A database has one host that prepares it. The host says which, in its `appsettings.json` (the names of the connection strings, separated by semicolons):

```json
"SeedStartupConnections": "EfCore-IdentityService;EfCore-HistoryService"
```

| Setting | The host prepares |
|---|---|
| absent | every database it has (a service with one host) |
| `"EfCore-IdentityService"` | that one; the others it only uses, and waits for (its readiness checks) |
| `""` | none |

An example: the identity service has a REST host and, later, a WCF host. The REST host owns `EfCore-HistoryService` (PostgreSQL), the WCF host owns `EfCore-IdentityService` (SQL Server): each seeds its own database, and the other never does. The two settings are the only difference between them; they are in the `appsettings.json`
because who owns a database is the same in every environment. In Kubernetes the same is an environment variable (`SeedStartupConnections`).

## The steps

A service says what prepares its database in the **registration of its persistence**, so the `Program.cs` of a host has nothing to write about it:

```csharp
// the persistence of the order service
public static IServiceCollection AddOrderPersistence(this IServiceCollection services, IConfiguration configuration)
{
    services.AddBaseEfCore<OrderDbContext>(configuration, providers => providers.UsePostgres(), "postgres", "EfCore-OrderService");
    services.AddEfCoreRepository<OrderDbContext, Order, Guid>();

    services.AddSchemaStartupStep<OrderDbContext>("EfCore-OrderService");                       // the schema
    services.AddDatabaseStartupStep("EfCore-OrderService", "order statuses", async (scope, cancellationToken) =>   // the seed
    {
        OrderDbContext db = scope.GetRequiredService<OrderDbContext>();
        if (!await db.Statuses.AnyAsync(cancellationToken))     // look, then add: safe, because one pod at a time is here
        {
            db.Statuses.AddRange(...);
            await db.SaveChangesAsync(cancellationToken);
        }
    });
    return services;
}

// MongoDB, or anything that is not a context: the same call, the name of the connection string says which database
services.AddDatabaseStartupStep("Mongo-ShipmentService", "indexes", async (scope, cancellationToken) => { ... });
```

What happens when the host starts:

- the steps run **in the order they were added**, each in a scope of its own (`scope` has the context, the current tenant, the options), **before any hosted service starts** (the consumers of the event bus among them: a message never meets a table that is not there yet) and before the host listens;
- a step runs only on the host that owns its database; of the pods of that host one at a time, under the lock of the database; it is tried again while the database is not there (taking the lock is part of the attempt);
- a step that still fails after that stops the host from starting (the platform restarts the pod).

**Write a step so that running it again changes nothing** (look before you add; `CreateIndex` is already like that): the pod that gets the lock after another finds the work done.

A step that needs to be run by hand (a tool, a test) can use the same rules without the host: `await services.RunDatabaseStepAsync("EfCore-OrderService", "name", async cancellationToken => { ... })`, and for a context `EnsureDatabaseCreatedAsync<TContext>()`, `RunContextStepAsync<TContext>` and `RunScopedContextStepAsync<TContext>`.

## The locks

| Database | Lock | A pod that dies |
|---|---|---|
| SQL Server | `sp_getapplock` of a session on `master` | the connection closes, the lock goes |
| PostgreSQL | an advisory lock of a session on `postgres` | the connection closes, the lock goes |
| MySQL | `GET_LOCK` of a session, no database selected | the connection closes, the lock goes |
| MongoDB | a lease document in `_startup_locks` (a minute, renewed every twenty seconds) | the lease ends |

A provider that has no lock (a provider written for a service) runs the step without one: it is only retried.
