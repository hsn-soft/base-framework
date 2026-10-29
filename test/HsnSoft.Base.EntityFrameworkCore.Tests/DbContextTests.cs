using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class DbContextTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("hsn-ef-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private sealed class Item
    {
        public int Id { get; set; }
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    private static ServiceProvider BuildProvider(string dataSource)
    {
        IServiceCollection services = new ServiceCollection().AddLogging();
        services.AddDbContext<TestContext>(options => options.UseSqlite($"Data Source={dataSource}"));
        services.AddDbContextHealthCheck<TestContext>("test-db");
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EnsureDatabaseCreated_CreatesTheSchema_AndIsRepeatable()
    {
        await using ServiceProvider provider = BuildProvider(Path.Combine(_directory, "app.db"));

        await provider.EnsureDatabaseCreatedAsync<TestContext>();
        await provider.EnsureDatabaseCreatedAsync<TestContext>();

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        TestContext context = scope.ServiceProvider.GetRequiredService<TestContext>();
        context.Items.Add(new Item());
        Assert.Equal(1, await context.SaveChangesAsync());
    }

    [Fact]
    public async Task HealthCheck_IsHealthy_WhenTheDatabaseIsReachable()
    {
        await using ServiceProvider provider = BuildProvider(Path.Combine(_directory, "app.db"));
        await provider.EnsureDatabaseCreatedAsync<TestContext>();

        HealthCheckResult result = await Check(provider);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task HealthCheck_IsUnhealthy_WhenTheDatabaseCannotBeReached()
    {
        await using ServiceProvider provider = BuildProvider(Path.Combine(_directory, "missing", "app.db"));

        HealthCheckResult result = await Check(provider);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task HealthCheck_IsRegisteredAsAReadinessCheck()
    {
        await using ServiceProvider provider = BuildProvider(Path.Combine(_directory, "app.db"));

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("test-db", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public async Task NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => DbContextExtensions.AddDbContextHealthCheck<TestContext>(null!, "x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => DbContextExtensions.EnsureDatabaseCreatedAsync<TestContext>(null!));
    }

    private static async Task<HealthCheckResult> Check(ServiceProvider provider)
    {
        HealthCheckRegistration registration = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Single();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IHealthCheck check = registration.Factory(scope.ServiceProvider);
        return await check.CheckHealthAsync(new HealthCheckContext { Registration = registration });
    }
}
