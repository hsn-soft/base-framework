using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Tests;

public class MongoRegistrationTests
{
    private static IConfiguration Configuration(string? connectionString)
        => new ConfigurationBuilder().AddInMemoryCollection(connectionString is null ? [] : new Dictionary<string, string?> { ["ConnectionStrings:Mongo"] = connectionString }).Build();

    [Fact]
    public void DatabaseNamedInTheConnectionString_IsRegistered()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseMongo(Configuration("mongodb://localhost:27099/acme"));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("acme", provider.GetRequiredService<IMongoDatabase>().DatabaseNamespace.DatabaseName);
        Assert.NotNull(provider.GetRequiredService<IMongoClient>());
    }

    [Fact]
    public void Guids_AreStoredAsStandardBinary()
    {
        new ServiceCollection().AddBaseMongo(Configuration("mongodb://localhost:27099/acme"));

        GuidSerializer serializer = Assert.IsType<GuidSerializer>(BsonSerializer.LookupSerializer<Guid>());

        Assert.Equal(GuidRepresentation.Standard, serializer.GuidRepresentation);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddBaseMongo(Configuration("mongodb://localhost:27099/acme"));
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("mongo", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseMongo(Configuration(null)));

        Assert.Contains("ConnectionStrings:Mongo", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionStringWithoutDatabase_IsAnError()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseMongo(Configuration("mongodb://localhost:27099")));

        Assert.Contains("database name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => MongoServiceCollectionExtensions.AddBaseMongo(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseMongo(null!));
    }

    private sealed class Probe(Guid id) : HsnSoft.Base.Domain.Entities.Entity<Guid>(id);

    private static IConfiguration TwoConnections()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Mongo-ProductService"] = "mongodb://localhost:27099/abc_product",
            ["ConnectionStrings:Mongo-StockService"] = "mongodb://localhost:27098/abc_stock",
        }).Build();

    [Fact]
    public void ASecondMongoDb_IsRegisteredUnderItsKey_BesideTheFirst()
    {
        IConfiguration configuration = TwoConnections();
        IServiceCollection services = new ServiceCollection().AddLogging()
            .AddBaseMongo(configuration, "Mongo-ProductService")
            .AddBaseMongo(configuration, "Mongo-StockService", "stock");
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("abc_product", provider.GetRequiredService<IMongoDatabase>().DatabaseNamespace.DatabaseName);
        Assert.Equal("abc_stock", provider.GetRequiredKeyedService<IMongoDatabase>("stock").DatabaseNamespace.DatabaseName);
        Assert.NotSame(provider.GetRequiredService<IMongoClient>(), provider.GetRequiredKeyedService<IMongoClient>("stock"));
        Assert.Equal(["mongo", "mongo-stock"], provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Select(registration => registration.Name).Order());
        Assert.All(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations, registration => Assert.Contains("ready", registration.Tags));
        Assert.NotNull(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.Single(registration => registration.Name == "mongo-stock").Factory(provider));
    }

    [Fact]
    public void ARepository_UsesTheDatabaseOfItsKey()
    {
        IConfiguration configuration = TwoConnections();
        IServiceCollection services = new ServiceCollection().AddLogging()
            .AddBaseMongo(configuration, "Mongo-ProductService")
            .AddBaseMongo(configuration, "Mongo-StockService", "stock")
            .AddMongoRepository<Probe, Guid>("probes", "stock");
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        object repository = scope.ServiceProvider.GetRequiredService<HsnSoft.Base.Domain.Repositories.IRepository<Probe, Guid>>();
        IMongoCollection<Probe> collection = (IMongoCollection<Probe>)repository.GetType().GetProperty("Collection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(repository)!;

        Assert.Equal("abc_stock.probes", collection.CollectionNamespace.FullName);
    }

    [Fact]
    public void ABlankKey_IsRefused()
        => Assert.Throws<ArgumentException>(() => new ServiceCollection().AddBaseMongo(TwoConnections(), "Mongo-StockService", " "));
}
