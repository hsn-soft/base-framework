using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(Configuration("Server=localhost,1499;Database=x;User Id=u;Password=p;TrustServerCertificate=True"));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        DatabaseFacade database = scope.ServiceProvider.GetRequiredService<TestContext>().Database;

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", database.ProviderName);
        Assert.True(database.CreateExecutionStrategy().RetriesOnFailure);
    }

    [Fact]
    public void TheProvider_ConfiguresAContextWithTheDefaults_WhenItIsAskedWithoutTheTuning()
    {
        EfCoreProviderOptions providers = new();
        IEfCoreProvider provider = Assert.Single(providers.UseSqlServer().Providers);
        DbContextOptionsBuilder builder = new();

        provider.Configure(builder, "Host=localhost;Database=x");

        Assert.True(builder.IsConfigured);
        Assert.IsAssignableFrom<IStartupLockProvider>(provider);
    }

    [Fact]
    public void TheRetriesAndTheCommandTimeout_AreBoundedByDefault_AndAreTheSettingsOfTheSection()
    {
        using ServiceProvider bounded = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(Configuration("Server=localhost,5499;Database=x;User Id=u;Password=p;TrustServerCertificate=True")).BuildServiceProvider();
        using IServiceScope boundedScope = bounded.CreateScope();
        DatabaseFacade defaults = boundedScope.ServiceProvider.GetRequiredService<TestContext>().Database;

        IConfiguration settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SqlServer"] = "Server=localhost,5499;Database=x;User Id=u;Password=p;TrustServerCertificate=True",
            ["EntityFramework:CommandTimeout"] = "00:00:12",
            ["EntityFramework:MaxRetryCount"] = "0",
        }).Build();
        using ServiceProvider custom = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(settings).BuildServiceProvider();
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
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(Configuration("Server=localhost,1499;Database=x;User Id=u;Password=p;TrustServerCertificate=True"));
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("sqlserver", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ConnectionNameIsConfigurable()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Other"] = "Server=localhost,1499;Database=x;User Id=u;Password=p;TrustServerCertificate=True" }).Build();

        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseSqlServer<TestContext>(configuration, "Other");

        Assert.NotEmpty(services);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseSqlServer<TestContext>(Configuration(null)));

        Assert.Contains("ConnectionStrings:SqlServer", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddBaseSqlServer<TestContext>(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseSqlServer<TestContext>(null!));
    }
}
