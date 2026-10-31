using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EntityFrameworkCore.PostgreSql.Tests;

public class RegistrationTests
{
    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options);

    private static IConfiguration Configuration(string? connectionString)
        => new ConfigurationBuilder().AddInMemoryCollection(connectionString is null ? [] : new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = connectionString }).Build();

    [Fact]
    public void DbContext_UsesTheProvider_AndRetriesTransientFailures()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(Configuration("Host=localhost;Port=5499;Database=x;Username=u;Password=p"));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        DatabaseFacade database = scope.ServiceProvider.GetRequiredService<TestContext>().Database;

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", database.ProviderName);
        Assert.True(database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void TheRetriesAndTheCommandTimeout_AreBoundedByDefault_AndAreTheSettingsOfTheSection()
    {
        using ServiceProvider bounded = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(Configuration("Host=localhost;Port=5499;Database=x;Username=u;Password=p")).BuildServiceProvider();
        using IServiceScope boundedScope = bounded.CreateScope();
        DatabaseFacade defaults = boundedScope.ServiceProvider.GetRequiredService<TestContext>().Database;

        IConfiguration settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5499;Database=x;Username=u;Password=p",
            ["EntityFramework:CommandTimeout"] = "00:00:12",
            ["EntityFramework:MaxRetryCount"] = "0",
        }).Build();
        using ServiceProvider custom = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(settings).BuildServiceProvider();
        using IServiceScope customScope = custom.CreateScope();
        DatabaseFacade configured = customScope.ServiceProvider.GetRequiredService<TestContext>().Database;

        Assert.Equal(30, defaults.GetCommandTimeout());
        Assert.True(defaults.CreateExecutionStrategy().RetriesOnFailure);
        Assert.Equal(12, configured.GetCommandTimeout());
        Assert.False(configured.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(Configuration("Host=localhost;Port=5499;Database=x;Username=u;Password=p"));
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("postgres", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ConnectionNameIsConfigurable()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Other"] = "Host=localhost;Port=5499;Database=x;Username=u;Password=p" }).Build();

        IServiceCollection services = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(configuration, "Other");

        Assert.NotEmpty(services);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBasePostgres<TestContext>(Configuration(null)));

        Assert.Contains("ConnectionStrings:Postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PostgresServiceCollectionExtensions.AddBasePostgres<TestContext>(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBasePostgres<TestContext>(null!));
    }
}
