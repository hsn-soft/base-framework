using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreProviderTests
{
    private sealed class ProviderContext(DbContextOptions<ProviderContext> options) : DbContext(options);

    private sealed class OtherContext(DbContextOptions<OtherContext> options) : DbContext(options);

    // a provider that is SQLite under another name: what is checked is which one runs and with which connection string
    private sealed class FakeProvider(string name) : IEfCoreProvider
    {
        public string Name => name;

        public void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSqlite(connectionString);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value)).Build();

    private static string? ConnectionOf(IServiceCollection services)
    {
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ProviderContext>().Database.GetConnectionString();
    }

    private static void Both(EfCoreProviderOptions providers) => providers.Add(new FakeProvider("alpha")).Add(new FakeProvider("beta"));

    private static readonly IConfiguration s_connections = Configuration(("ConnectionStrings:EfCoreA", "Data Source=a.db"), ("ConnectionStrings:EfCoreB", "Data Source=b.db"));

    [Theory]
    [InlineData("alpha", "EfCoreA", "Data Source=a.db")]
    [InlineData("BETA", "EfCoreB", "Data Source=b.db")]
    [InlineData(" beta ", "EfCoreA", "Data Source=a.db")]
    public void TheProviderAndTheConnectionName_AreWhatTheCodeSays(string provider, string connectionName, string expected)
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(s_connections, Both, provider, connectionName);

        Assert.Equal(expected, ConnectionOf(services));
    }

    [Fact]
    public void TheCheckIsNamedAfterTheProvider_AndIsReady()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(s_connections, Both, "beta", "EfCoreB");
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("beta", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void ASecondContextOnTheSameProvider_HasACheckNamedAfterItToo()
    {
        IServiceCollection services = new ServiceCollection().AddLogging()
            .AddBaseEfCore<ProviderContext>(s_connections, Both, "beta", "EfCoreB")
            .AddBaseEfCore<OtherContext>(s_connections, Both, "beta", "EfCoreA")
            .AddBaseEfCore<ProviderContext>(s_connections, Both, "alpha", "EfCoreA");
        using ServiceProvider provider = services.BuildServiceProvider();

        string[] names = [.. provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(registration => registration.Name)];

        Assert.Equal(["beta", "beta-OtherContext", "alpha"], names);
    }

    [Fact]
    public void ASecondProviderOfTheSameName_ReplacesTheFirst()
    {
        EfCoreProviderOptions options = new();
        FakeProvider second = new("ALPHA");

        options.Add(new FakeProvider("alpha")).Add(second);

        Assert.Same(second, Assert.Single(options.Providers));
    }

    [Fact]
    public void TheMistakes_AreToldWithWhatIsOffered()
    {
        InvalidOperationException unknown = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(s_connections, Both, "oracle", "EfCoreA"));
        InvalidOperationException noConnection = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(s_connections, Both, "beta", "Missing"));
        InvalidOperationException none = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(s_connections, _ => { }, "alpha", "EfCoreA"));

        Assert.Contains("'oracle'", unknown.Message, StringComparison.Ordinal);
        Assert.Contains("alpha, beta", unknown.Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:Missing", noConnection.Message, StringComparison.Ordinal);
        Assert.Contains("No database provider", none.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullAndBlankArguments_AreRefused()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseEfCore<ProviderContext>(s_connections, Both, "alpha", "EfCoreA"));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(null!, Both, "alpha", "EfCoreA"));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(s_connections, null!, "alpha", "EfCoreA"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(s_connections, Both, " ", "EfCoreA"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(s_connections, Both, "alpha", " "));
        Assert.Throws<ArgumentNullException>(() => new EfCoreProviderOptions().Add(null!));
    }
}
