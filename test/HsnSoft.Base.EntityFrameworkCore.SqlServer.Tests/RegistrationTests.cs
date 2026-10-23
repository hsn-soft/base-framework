using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EntityFrameworkCore.SqlServer.Tests;

public class RegistrationTests
{
    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options);

    private static IConfiguration Configuration(string? connectionString)
        => new ConfigurationBuilder().AddInMemoryCollection(connectionString is null ? [] : new Dictionary<string, string?> { ["ConnectionStrings:SqlServer"] = connectionString }).Build();

    [Fact]
    public void DbContext_UsesTheProvider_AndRetriesTransientFailures()
    {
        var services = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(Configuration("Server=localhost,1499;Database=x;User Id=u;Password=p;TrustServerCertificate=True"));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<TestContext>().Database;

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", database.ProviderName);
        Assert.True(database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        var services = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(Configuration("Server=localhost,1499;Database=x;User Id=u;Password=p;TrustServerCertificate=True"));
        using var provider = services.BuildServiceProvider();

        var registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("sqlserver", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ConnectionNameIsConfigurable()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Other"] = "Server=localhost,1499;Database=x;User Id=u;Password=p;TrustServerCertificate=True" }).Build();

        var services = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(configuration, "Other");

        Assert.NotEmpty(services);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseSqlServer<TestContext>(Configuration(null)));

        Assert.Contains("ConnectionStrings:SqlServer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddBaseSqlServer<TestContext>(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseSqlServer<TestContext>(null!));
    }
}
