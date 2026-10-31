using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.IntegrationTests.Data;

/// <summary>The contexts of the framework retry the commands that fail in a way that may pass, and a retrying strategy cannot run a transaction that the code starts by itself.</summary>
[Collection(Collections.Postgres)]
public sealed class TransactionTests(PostgresFixture postgres)
{
    private async Task<IHost> StartAsync()
    {
        IHost host = await Support.StartHostAsync(services => services.AddBaseEfCore<ItemContext>(StartupChecks.Configuration(postgres.NewConnectionString()), providers => providers.UsePostgres(), EfCoreProviderNames.Postgres, StartupChecks.Connection));
        await host.Services.EnsureDatabaseCreatedAsync<ItemContext>();
        return host;
    }

    [Fact]
    public async Task ATransactionStartedByTheCode_IsRefusedByTheRetryingStrategy()
    {
        using IHost host = await StartAsync();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();

        // the transaction begins; the first command in it is what the strategy refuses
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
        context.Items.Add(new Item { Name = "refused" });
        InvalidOperationException refused = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        Assert.Contains("execution strategy", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_CommitsTheWork_AsOne()
    {
        using IHost host = await StartAsync();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();

        int added = await context.ExecuteInTransactionAsync(async cancellationToken =>
        {
            context.Items.Add(new Item { Name = "one" });
            await context.SaveChangesAsync(cancellationToken);
            context.Items.Add(new Item { Name = "two" });
            await context.SaveChangesAsync(cancellationToken);
            return 2;
        });

        Assert.Equal(2, added);
        await using AsyncServiceScope other = host.Services.CreateAsyncScope();
        Assert.Equal(2, await other.ServiceProvider.GetRequiredService<ItemContext>().Items.CountAsync());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBackEverything_WhenTheWorkFails()
    {
        using IHost host = await StartAsync();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.ExecuteInTransactionAsync<int>(async cancellationToken =>
        {
            context.Items.Add(new Item { Name = "lost" });
            await context.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("the work failed after its first save");
        }));

        await using AsyncServiceScope other = host.Services.CreateAsyncScope();
        Assert.Equal(0, await other.ServiceProvider.GetRequiredService<ItemContext>().Items.CountAsync());
    }

    [Fact]
    public async Task TheCommandTimeoutAndTheRetries_AreTheSettingsOfTheSection()
    {
        IHost host = await Support.StartHostAsync(services => services.AddBaseEfCore<ItemContext>(
            Support.Configuration(($"ConnectionStrings:{StartupChecks.Connection}", postgres.NewConnectionString()), ("EntityFramework:CommandTimeout", "00:00:07"), ("EntityFramework:MaxRetryCount", "1"), ("EntityFramework:MaxRetryDelay", "00:00:01")),
            providers => providers.UsePostgres(),
            EfCoreProviderNames.Postgres,
            StartupChecks.Connection));
        using (host)
        {
            await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
            ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();

            Assert.Equal(7, context.Database.GetCommandTimeout());
        }

        using IHost defaults = await StartAsync();
        await using AsyncServiceScope other = defaults.Services.CreateAsyncScope();
        Assert.Equal(30, other.ServiceProvider.GetRequiredService<ItemContext>().Database.GetCommandTimeout());
    }
}
