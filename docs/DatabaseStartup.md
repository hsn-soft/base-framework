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

### A schema that changes: migrations

`AddSchemaStartupStep` makes the tables of a database that is **not there** and does nothing to one that is (it is `EnsureCreated`), so a table added later, a column or a rename never reaches a database that exists. A service whose schema changes uses **`AddMigrationStartupStep<TContext>(connection)`
instead** (never both): it applies the migrations of the context that the database has not had, in their order, and makes the database first when it is not there. It is a step like the others: only the host that owns the database, one pod at a time under the lock, so two pods never run the same migration, and it runs again
without changing anything. By hand: `services.MigrateDatabaseAsync<TContext>()`.

The migrations are the service's: they live in the project of its context (`dotnet ef migrations add <Name> --project <the persistence project> --startup-project <a host>`; the context needs the options `AddBaseEfCore` gives it, so the tool starts from a host or from an `IDesignTimeDbContextFactory` of the project), are
committed with the change that needs them, and the model snapshot is what EF compares the model with (a model that changed without a migration stops the migration, which is what is wanted). A database that was made by `AddSchemaStartupStep` has no history of migrations: the first migration would fail on a table that exists, so such a
database is made again, or its history is filled by hand once ([ADR-0131](adr/0131-a-schema-that-evolves-is-migrated-by-a-startup-step.md)).

## The locks

| Database | Lock | A pod that dies |
|---|---|---|
| SQL Server | `sp_getapplock` of a session on `master` | the connection closes, the lock goes |
| PostgreSQL | an advisory lock of a session on `postgres` | the connection closes, the lock goes |
| MySQL | `GET_LOCK` of a session, no database selected | the connection closes, the lock goes |
| MongoDB | a lease document in `_startup_locks` (a minute, renewed every twenty seconds) | the lease ends |

A provider that has no lock (a provider written for a service) runs the step without one: it is only retried.

## Seeders: the data a database starts with

An `IDataSeeder` writes a piece of the starting data (the tenants, the catalogue of permissions, the first administrator, the data of a sample). `AddDataSeeder<TSeeder>(connectionName)` makes it a step of the database, so it runs
on the host that owns the database, one pod at a time under the lock, in the order the seeders were added and after `AddSchemaStartupStep`; the container makes it in a scope of its own, so it takes what it needs in its constructor.

```csharp
services.AddSchemaStartupStep<IdentityDbContext>(connection)
        .AddDataSeeder<TenantsSeeder>(connection)          // in this order
        .AddDataSeeder<SampleIdentitySeeder>(connection);

internal sealed class TenantsSeeder(IdentityDbContext db) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken) { /* add what is not there; running it twice changes nothing */ }
}
```

A seeder is written to be run again (every pod that gets the lock after the first, every start): it checks what is there and adds only what is not. An environment that must not have a seeder (the people of a sample, in production)
lists it by the name of its class: `Seeding:Disabled:0 = SampleIdentitySeeder`. See [ADR-0125](adr/0125-the-data-a-database-starts-with-is-written-by-seeders-that-are-steps-of-the-database.md).
