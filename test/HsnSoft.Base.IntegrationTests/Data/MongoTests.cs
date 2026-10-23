using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class MongoTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private sealed class Item
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public async Task Database_IsUsable_GuidsAreStandardBinary_AndTheServiceIsReady()
    {
        var url = new MongoUrlBuilder(_container.GetConnectionString()) { DatabaseName = "it", AuthenticationSource = "admin" }.ToString();
        var configuration = Support.Configuration(("ConnectionStrings:Mongo", url));
        using var host = await Support.StartHostAsync(services => services.AddBaseMongo(configuration));
        var database = host.Services.GetRequiredService<IMongoDatabase>();
        var item = new Item { Id = Guid.NewGuid() };

        await database.GetCollection<Item>("items").InsertOneAsync(item);

        var raw = await database.GetCollection<BsonDocument>("items").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal(BsonBinarySubType.UuidStandard, raw["_id"].AsBsonBinaryData.SubType);
        Assert.Equal(item.Id, raw["_id"].AsGuid);
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
    }
}
