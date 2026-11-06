using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.MySql;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MongoDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.IntegrationTests.Data;

/// <summary>
/// Many pods start together and prepare the same database: the schema and the seed data. The seed here is the kind that goes wrong without a lock: it looks whether there is a row, waits, and adds one.
/// Four pods (four hosts) do it at once; under the startup lock of the database exactly one row is there, and a host that does not own the database does nothing.
/// </summary>
internal static class StartupChecks
{
    public const string Connection = "Db";

    public static IConfiguration Configuration(string connectionString, string? owns = null)
    {
        List<(string, string?)> settings = [($"ConnectionStrings:{Connection}", connectionString)];
        if (owns is not null)
        {
            settings.Add((StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting, owns));
        }

        return Support.Configuration([.. settings]);
    }

    /// <summary>The seed that needs the lock: look, wait, add.</summary>
    public static Task SeedAsync(IServiceProvider services) => services.RunContextStepAsync<ItemContext>("seed", async (context, cancellationToken) =>
    {
        if (!await context.Items.AnyAsync(cancellationToken))
        {
            await Task.Delay(300, cancellationToken);
            context.Items.Add(new Item { Name = "seed" });
            await context.SaveChangesAsync(cancellationToken);
        }
    });

    public static async Task<int> PodsAsync(string connectionString, Action<EfCoreProviderOptions> use, string provider, int pods)
    {
        IHost[] hosts = await Task.WhenAll(Enumerable.Range(0, pods).Select(_ => Support.StartHostAsync(services =>
            services.AddBaseEfCore<ItemContext>(Configuration(connectionString), use, provider, Connection))));
        try
        {
            await Task.WhenAll(hosts.Select(async host =>
            {
                await host.Services.EnsureDatabaseCreatedAsync<ItemContext>();
                await SeedAsync(host.Services);
            }));

            return await CountAsync(hosts[0].Services);
        }
        finally
        {
            foreach (IHost host in hosts)
            {
                host.Dispose();
            }
        }
    }

    public static async Task<int> CountAsync(IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ItemContext>().Items.CountAsync();
    }

    public static async Task AssertNotOwnerLeavesTheDatabaseAloneAsync(string connectionString, Action<EfCoreProviderOptions> use, string provider)
    {
        using IHost visitor = await Support.StartHostAsync(services => services.AddBaseEfCore<ItemContext>(Configuration(connectionString, owns: string.Empty), use, provider, Connection));

        await visitor.Services.EnsureDatabaseCreatedAsync<ItemContext>();
        await SeedAsync(visitor.Services);

        // nothing was created by the host that is not the owner: the owner makes it, and finds it as it is
        using IHost owner = await Support.StartHostAsync(services => services.AddBaseEfCore<ItemContext>(Configuration(connectionString, owns: Connection), use, provider, Connection));
        await owner.Services.EnsureDatabaseCreatedAsync<ItemContext>();
        Assert.Equal(0, await CountAsync(owner.Services));
        await SeedAsync(owner.Services);
        Assert.Equal(1, await CountAsync(owner.Services));
    }
}

/// <summary>A hosted service that starts after the steps of the databases: it asks what it finds.</summary>
internal sealed class WhatTheConsumerFinds(IServiceProvider services) : IHostedService
{
    public int Found { get; private set; } = -1;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // a consumer of the event bus: the first thing it does is to use the database
        Found = await StartupChecks.CountAsync(services);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

[Collection(Collections.Postgres)]
public sealed class PostgreSqlStartupTests(PostgresFixture postgres)
{
    [Fact]
    public async Task TheStepsOfThePersistence_RunWhenTheHostStarts_BeforeAnyHostedService_InFourHostsAtOnce()
    {
        string connectionString = postgres.NewConnectionString();
        List<WhatTheConsumerFinds> consumers = [];
        IHost[] hosts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Support.StartHostAsync(services =>
        {
            services.AddBaseEfCore<ItemContext>(StartupChecks.Configuration(connectionString), providers => providers.UsePostgres(), EfCoreProviderNames.Postgres, StartupChecks.Connection);
            // what the persistence of a service adds: the schema, then the seed; nothing in the Program.cs of the host
            services.AddSchemaStartupStep<ItemContext>(StartupChecks.Connection);
            services.AddDatabaseStartupStep(StartupChecks.Connection, "seed", async (scope, cancellationToken) =>
            {
                ItemContext context = scope.GetRequiredService<ItemContext>();
                if (!await context.Items.AnyAsync(cancellationToken))
                {
                    await Task.Delay(300, cancellationToken);
                    context.Items.Add(new Item { Name = "seed" });
                    await context.SaveChangesAsync(cancellationToken);
                }
            });
            // registered after the persistence, as the event bus is: it starts after the steps all the same
            services.AddSingleton<IHostedService>(provider =>
            {
                WhatTheConsumerFinds started = new(provider);
                lock (consumers)
                {
                    consumers.Add(started);
                }

                return started;
            });
        })));
        try
        {
            Assert.Equal(1, await StartupChecks.CountAsync(hosts[0].Services));
            Assert.Equal(4, consumers.Count);
            Assert.All(consumers, consumer => Assert.True(consumer.Found >= 1, "a hosted service started before the database was prepared"));
        }
        finally
        {
            foreach (IHost host in hosts)
            {
                host.Dispose();
            }
        }
    }

    [Fact]
    public async Task FourPodsAtOnce_PrepareTheDatabaseOnce_UnderTheAdvisoryLock()
        => Assert.Equal(1, await StartupChecks.PodsAsync(postgres.NewConnectionString(), providers => providers.UsePostgres(), EfCoreProviderNames.Postgres, 4));

    [Fact]
    public async Task AHostThatDoesNotOwnTheDatabase_LeavesItToTheOwner()
        => await StartupChecks.AssertNotOwnerLeavesTheDatabaseAloneAsync(postgres.NewConnectionString(), providers => providers.UsePostgres(), EfCoreProviderNames.Postgres);

    [Fact]
    public async Task WithoutTheLock_TheSameSeedMakesTheRowMoreThanOnce()
    {
        string connectionString = postgres.NewConnectionString();
        IHost[] hosts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Support.StartHostAsync(services =>
            services.AddBaseEfCore<ItemContext>(StartupChecks.Configuration(connectionString), providers => providers.UsePostgres(), EfCoreProviderNames.Postgres, StartupChecks.Connection))));
        try
        {
            await hosts[0].Services.EnsureDatabaseCreatedAsync<ItemContext>();

            // the same seed as StartupChecks.SeedAsync, as a plain step: not owned, not locked
            await Task.WhenAll(hosts.Select(host => host.Services.RunStartupStepAsync("seed", async cancellationToken =>
            {
                await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
                ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();
                if (!await context.Items.AnyAsync(cancellationToken))
                {
                    await Task.Delay(300, cancellationToken);
                    context.Items.Add(new Item { Name = "seed" });
                    await context.SaveChangesAsync(cancellationToken);
                }
            })));

            Assert.True(await StartupChecks.CountAsync(hosts[0].Services) > 1);
        }
        finally
        {
            foreach (IHost host in hosts)
            {
                host.Dispose();
            }
        }
    }

    [Fact]
    public async Task TheLock_IsHeldByOneAtATime_AndFreeAgainWhenItIsDisposed()
    {
        PostgresStartupLock startupLock = new(postgres.NewConnectionString());
        IAsyncDisposable first = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);

        await Assert.ThrowsAsync<TimeoutException>(() => startupLock.AcquireAsync("one", TimeSpan.FromSeconds(1), CancellationToken.None));
        await using IAsyncDisposable other = await startupLock.AcquireAsync("another", TimeSpan.FromSeconds(5), CancellationToken.None);
        await first.DisposeAsync();
        await using IAsyncDisposable again = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);
    }
}

[Collection(Collections.SqlServer)]
public sealed class SqlServerStartupTests(SqlServerFixture sqlServer)
{
    [Fact]
    public async Task FourPodsAtOnce_PrepareTheDatabaseOnce_UnderTheApplicationLock()
        => Assert.Equal(1, await StartupChecks.PodsAsync(sqlServer.NewConnectionString(), providers => providers.UseSqlServer(), EfCoreProviderNames.SqlServer, 4));

    [Fact]
    public async Task AHostThatDoesNotOwnTheDatabase_LeavesItToTheOwner()
        => await StartupChecks.AssertNotOwnerLeavesTheDatabaseAloneAsync(sqlServer.NewConnectionString(), providers => providers.UseSqlServer(), EfCoreProviderNames.SqlServer);

    [Fact]
    public async Task TheLock_IsHeldByOneAtATime_AndFreeAgainWhenItIsDisposed()
    {
        SqlServerStartupLock startupLock = new(sqlServer.NewConnectionString());
        IAsyncDisposable first = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);

        await Assert.ThrowsAsync<TimeoutException>(() => startupLock.AcquireAsync("one", TimeSpan.FromSeconds(1), CancellationToken.None));
        await first.DisposeAsync();
        await using IAsyncDisposable again = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);
    }

    [Fact]
    public async Task AWaiter_WaitsForAHolderThatTakesLongerThanTheDefaultsOfTheClient()
    {
        SqlServerStartupLock startupLock = new(sqlServer.NewConnectionString());
        IAsyncDisposable holder = await startupLock.AcquireAsync("slow", TimeSpan.FromSeconds(5), CancellationToken.None);
        Task<IAsyncDisposable> waiter = startupLock.AcquireAsync("slow", TimeSpan.FromSeconds(120), CancellationToken.None);

        // the holder is a pod that is creating a database: it holds the lock for much longer than a server waits in one call (and, in an emulator, longer than it was told to)
        await Task.Delay(TimeSpan.FromSeconds(15));
        Assert.False(waiter.IsCompleted);
        await holder.DisposeAsync();

        await using IAsyncDisposable got = await waiter;
    }
}

[Collection(Collections.MySql)]
public sealed class MySqlStartupTests(MySqlFixture mySql)
{
    [Fact]
    public async Task FourPodsAtOnce_PrepareTheDatabaseOnce_UnderTheNamedLock()
        => Assert.Equal(1, await StartupChecks.PodsAsync(mySql.NewConnectionString(), providers => providers.UseMySql(), EfCoreProviderNames.MySql, 4));

    [Fact]
    public async Task TheLock_IsHeldByOneAtATime_AndFreeAgainWhenItIsDisposed()
    {
        MySqlStartupLock startupLock = new(mySql.NewConnectionString());
        IAsyncDisposable first = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);

        await Assert.ThrowsAsync<TimeoutException>(() => startupLock.AcquireAsync("one", TimeSpan.FromSeconds(1), CancellationToken.None));
        await first.DisposeAsync();
        await using IAsyncDisposable again = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);
    }
}

[Collection(Collections.Mongo)]
public sealed class MongoStartupTests(MongoFixture mongo)
{
    private static async Task SeedAsync(IServiceProvider services) => await services.RunDatabaseStepAsync("Mongo", "seed", async cancellationToken =>
    {
        IMongoCollection<BsonDocument> items = services.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("items");
        if (await items.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken) == 0)
        {
            await Task.Delay(300, cancellationToken);
            await items.InsertOneAsync(new BsonDocument("name", "seed"), cancellationToken: cancellationToken);
        }
    });

    [Fact]
    public async Task FourPodsAtOnce_PrepareTheDatabaseOnce_UnderTheLeaseOfTheLockDocument()
    {
        string url = mongo.NewUrl();
        IHost[] hosts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Support.StartHostAsync(services => services.AddBaseMongo(Support.Configuration(("ConnectionStrings:Mongo", url)), "Mongo"))));
        try
        {
            await Task.WhenAll(hosts.Select(host => SeedAsync(host.Services)));

            IMongoDatabase database = hosts[0].Services.GetRequiredService<IMongoDatabase>();
            Assert.Equal(1, await database.GetCollection<BsonDocument>("items").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            // the lock is given back: its document is gone
            Assert.Equal(0, await database.GetCollection<BsonDocument>(MongoStartupLock.CollectionName).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        finally
        {
            foreach (IHost host in hosts)
            {
                host.Dispose();
            }
        }
    }

    [Fact]
    public async Task AHostThatDoesNotOwnTheDatabase_LeavesItToTheOwner()
    {
        string url = mongo.NewUrl();
        using IHost visitor = await Support.StartHostAsync(services => services.AddBaseMongo(Support.Configuration(("ConnectionStrings:Mongo", url), (StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting, string.Empty)), "Mongo"));

        await SeedAsync(visitor.Services);

        Assert.Equal(0, await visitor.Services.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("items").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task AHolderThatIsAliveRenewsItsLease_SoItKeepsTheLockForLongerThanTheLease()
    {
        MongoStartupLock startupLock = new(new MongoClient(mongo.NewUrl()).GetDatabase("locks"), lease: TimeSpan.FromSeconds(1), renew: TimeSpan.FromMilliseconds(250));
        IAsyncDisposable holder = await startupLock.AcquireAsync("long", TimeSpan.FromSeconds(5), CancellationToken.None);

        // two and a half leases later nobody has taken it over: it was renewed
        await Task.Delay(TimeSpan.FromMilliseconds(2500));
        await Assert.ThrowsAsync<TimeoutException>(() => startupLock.AcquireAsync("long", TimeSpan.FromMilliseconds(600), CancellationToken.None));
        await holder.DisposeAsync();

        await using IAsyncDisposable next = await startupLock.AcquireAsync("long", TimeSpan.FromSeconds(5), CancellationToken.None);
    }

    [Fact]
    public void TheTimesOfTheLease_AreChecked()
    {
        IMongoDatabase database = new MongoClient(mongo.NewUrl()).GetDatabase("locks");

        Assert.Throws<ArgumentNullException>(() => new MongoStartupLock(null!, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MongoStartupLock(database, TimeSpan.Zero, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MongoStartupLock(database, TimeSpan.FromSeconds(3), TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MongoStartupLock(database, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public async Task TheLock_IsHeldByOneAtATime_FreeAgainWhenItIsDisposed_AndTakenOverWhenItsLeaseEnds()
    {
        IMongoDatabase database = new MongoClient(mongo.NewUrl()).GetDatabase("locks");
        MongoStartupLock startupLock = new(database);
        IAsyncDisposable first = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);

        await Assert.ThrowsAsync<TimeoutException>(() => startupLock.AcquireAsync("one", TimeSpan.FromSeconds(1), CancellationToken.None));
        await first.DisposeAsync();
        await using IAsyncDisposable again = await startupLock.AcquireAsync("one", TimeSpan.FromSeconds(5), CancellationToken.None);

        // a holder that died: its lease is in the past, and the next one takes the lock over
        IMongoCollection<BsonDocument> locks = database.GetCollection<BsonDocument>(MongoStartupLock.CollectionName);
        await locks.ReplaceOneAsync(new BsonDocument("_id", "dead"), new BsonDocument { { "_id", "dead" }, { "owner", "gone" }, { "expiresAt", DateTime.UtcNow.AddMinutes(-1) } }, new ReplaceOptions { IsUpsert = true });
        await using IAsyncDisposable takenOver = await startupLock.AcquireAsync("dead", TimeSpan.FromSeconds(5), CancellationToken.None);
    }
}
