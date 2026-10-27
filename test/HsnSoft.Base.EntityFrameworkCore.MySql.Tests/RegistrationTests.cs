using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EntityFrameworkCore.MySql.Tests;

public class RegistrationTests
{
    private sealed class TestContext(DbContextOptions<TestContext> options) : DbContext(options);

    private static IConfiguration Configuration(string? connectionString)
        => new ConfigurationBuilder().AddInMemoryCollection(connectionString is null ? [] : new Dictionary<string, string?> { ["ConnectionStrings:MySql"] = connectionString }).Build();

    [Fact]
    public void DbContext_UsesTheProvider_AndRetriesTransientFailures()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseMySql<TestContext>(Configuration("Server=localhost;Port=3399;Database=x;User=u;Password=p"));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        DatabaseFacade database = scope.ServiceProvider.GetRequiredService<TestContext>().Database;

        Assert.Equal("MySql.EntityFrameworkCore", database.ProviderName);
        Assert.True(database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseMySql<TestContext>(Configuration("Server=localhost;Port=3399;Database=x;User=u;Password=p"));
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("mysql", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ConnectionNameIsConfigurable()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Other"] = "Server=localhost;Port=3399;Database=x;User=u;Password=p" }).Build();

        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseMySql<TestContext>(configuration, "Other");

        Assert.NotEmpty(services);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseMySql<TestContext>(Configuration(null)));

        Assert.Contains("ConnectionStrings:MySql", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => MySqlServiceCollectionExtensions.AddBaseMySql<TestContext>(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseMySql<TestContext>(null!));
    }
}
