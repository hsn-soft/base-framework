using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace HsnSoft.Base.Test.Integration.Fixtures;

public class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:latest").Build();

    public MongoFixture()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    private string _connectionString;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    /// <summary>Connection string targeting the given database on the container's MongoDB server.</summary>
    public string GetConnectionString(string databaseName)
    {
        var builder = new MongoUrlBuilder(_connectionString) { DatabaseName = databaseName, AuthenticationSource = "admin" };
        return builder.ToMongoUrl().ToString();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
