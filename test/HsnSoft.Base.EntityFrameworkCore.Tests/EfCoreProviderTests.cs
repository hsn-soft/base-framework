using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreProviderTests
{
    private sealed class ProviderContext(DbContextOptions<ProviderContext> options) : DbContext(options);

    // a provider that is SQLite under another name: what is checked is which one runs and with which connection string
    private sealed class FakeProvider(string name, string connectionName) : IEfCoreProvider
    {
        public string Name => name;

        public string ConnectionName => connectionName;

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

    private static void Both(EfCoreProviderOptions providers) => providers.Add(new FakeProvider("alpha", "Alpha")).Add(new FakeProvider("beta", "Beta"));

    [Theory]
    [InlineData("alpha", "Data Source=a.db")]
    [InlineData("BETA", "Data Source=b.db")]
    [InlineData(" beta ", "Data Source=b.db")]
    public void TheSettingSelectsTheProvider_AndItsConnectionString(string setting, string expected)
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(
            Configuration(("Database:Provider", setting), ("ConnectionStrings:Alpha", "Data Source=a.db"), ("ConnectionStrings:Beta", "Data Source=b.db")), Both);

        Assert.Equal(expected, ConnectionOf(services));
    }

    [Fact]
    public void WithOneProviderTheSettingMayBeLeftOut_AndTheCheckIsNamedAfterIt()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(
            Configuration(("ConnectionStrings:Alpha", "Data Source=a.db")), providers => providers.Add(new FakeProvider("alpha", "Alpha")));
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("alpha", registration.Name);
        Assert.Equal("Data Source=a.db", ConnectionOf(services));
    }

    [Fact]
    public void AGivenConnectionName_ReplacesTheProvidersOwn()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(
            Configuration(("ConnectionStrings:Other", "Data Source=o.db")), providers => providers.Add(new FakeProvider("alpha", "Alpha"), "Other"));

        Assert.Equal("Data Source=o.db", ConnectionOf(services));
    }

    [Fact]
    public void ASecondProviderOfTheSameName_ReplacesTheFirst()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(
            Configuration(("ConnectionStrings:Second", "Data Source=2.db")), providers => providers.Add(new FakeProvider("alpha", "First")).Add(new FakeProvider("ALPHA", "Second")));

        Assert.Equal("Data Source=2.db", ConnectionOf(services));
    }

    [Fact]
    public void TheMistakes_AreToldWithWhatIsOffered()
    {
        IConfiguration noSetting = Configuration(("ConnectionStrings:Alpha", "x"), ("ConnectionStrings:Beta", "y"));

        InvalidOperationException missing = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(noSetting, Both));
        InvalidOperationException unknown = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(Configuration(("Database:Provider", "oracle")), Both));
        InvalidOperationException noConnection = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(Configuration(("Database:Provider", "beta")), Both));
        InvalidOperationException none = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(noSetting, _ => { }));

        Assert.Contains("Database:Provider is required: the service offers alpha, beta", missing.Message, StringComparison.Ordinal);
        Assert.Contains("'oracle'", unknown.Message, StringComparison.Ordinal);
        Assert.Contains("alpha, beta", unknown.Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:Beta", noConnection.Message, StringComparison.Ordinal);
        Assert.Contains("No database provider", none.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AProviderAndAConnectionNameGivenByTheCode_AreUsedWhateverTheSettingsSay()
    {
        IConfiguration configuration = Configuration(("Database:Provider", "alpha"), ("ConnectionStrings:Alpha", "Data Source=a.db"), ("ConnectionStrings:EfCore-Own", "Data Source=own.db"));

        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseEfCore<ProviderContext>(configuration, Both, "beta", "EfCore-Own");

        Assert.Equal("Data Source=own.db", ConnectionOf(services));
        Assert.Contains("'oracle'", Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(configuration, Both, "oracle", "EfCore-Own")).Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:Missing", Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(configuration, Both, "beta", "Missing")).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRefused()
    {
        IConfiguration configuration = Configuration();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseEfCore<ProviderContext>(configuration, Both));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(null!, Both));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(configuration, null!));
        Assert.Throws<ArgumentNullException>(() => new EfCoreProviderOptions().Add(null!));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseEfCore<ProviderContext>(configuration, Both, "alpha", "Alpha"));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(null!, Both, "alpha", "Alpha"));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(configuration, null!, "alpha", "Alpha"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(configuration, Both, " ", "Alpha"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddBaseEfCore<ProviderContext>(configuration, Both, "alpha", " "));
    }
}
