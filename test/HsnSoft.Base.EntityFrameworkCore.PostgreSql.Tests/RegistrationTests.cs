using Microsoft.EntityFrameworkCore;
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
        var services = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(Configuration("Host=localhost;Port=5499;Database=x;Username=u;Password=p"));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<TestContext>().Database;

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", database.ProviderName);
        Assert.True(database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        var services = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(Configuration("Host=localhost;Port=5499;Database=x;Username=u;Password=p"));
        using var provider = services.BuildServiceProvider();

        var registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("postgres", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ConnectionNameIsConfigurable()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Other"] = "Host=localhost;Port=5499;Database=x;Username=u;Password=p" }).Build();

        var services = new ServiceCollection().AddLogging().AddBasePostgres<TestContext>(configuration, "Other");

        Assert.NotEmpty(services);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBasePostgres<TestContext>(Configuration(null)));

        Assert.Contains("ConnectionStrings:Postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PostgresServiceCollectionExtensions.AddBasePostgres<TestContext>(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBasePostgres<TestContext>(null!));
    }
}
