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
        var services = new ServiceCollection().AddLogging().AddBaseMongo(Configuration("mongodb://localhost:27099/acme"));
        using var provider = services.BuildServiceProvider();

        Assert.Equal("acme", provider.GetRequiredService<IMongoDatabase>().DatabaseNamespace.DatabaseName);
        Assert.NotNull(provider.GetRequiredService<IMongoClient>());
    }

    [Fact]
    public void Guids_AreStoredAsStandardBinary()
    {
        new ServiceCollection().AddBaseMongo(Configuration("mongodb://localhost:27099/acme"));

        var serializer = Assert.IsType<GuidSerializer>(BsonSerializer.LookupSerializer<Guid>());

        Assert.Equal(GuidRepresentation.Standard, serializer.GuidRepresentation);
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        var services = new ServiceCollection().AddLogging().AddBaseMongo(Configuration("mongodb://localhost:27099/acme"));
        using var provider = services.BuildServiceProvider();

        var registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("mongo", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void MissingConnectionString_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseMongo(Configuration(null)));

        Assert.Contains("ConnectionStrings:Mongo", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionStringWithoutDatabase_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddBaseMongo(Configuration("mongodb://localhost:27099")));

        Assert.Contains("database name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => MongoServiceCollectionExtensions.AddBaseMongo(null!, Configuration(null)));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddBaseMongo(null!));
    }
}
