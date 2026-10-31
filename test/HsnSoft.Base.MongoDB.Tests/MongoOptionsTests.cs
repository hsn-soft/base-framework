using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB.Tests;

public class MongoOptionsTests
{
    [Fact]
    public void TheWaits_AreBounded_UnlessTheConnectionStringSaysOtherwise()
    {
        MongoClientSettings settings = new MongoOptions().Apply("mongodb://mongo:27017/orders");

        Assert.Equal(TimeSpan.FromSeconds(10), settings.ServerSelectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(10), settings.ConnectTimeout);
        Assert.Equal(TimeSpan.FromMinutes(2), settings.SocketTimeout);
    }

    [Fact]
    public void AnOptionOfTheConnectionString_IsTheConnectionStrings()
    {
        MongoClientSettings settings = new MongoOptions().Apply("mongodb://mongo:27017/orders?serverSelectionTimeoutMS=3000&connectTimeoutMS=4000&socketTimeoutMS=5000");

        Assert.Equal(TimeSpan.FromSeconds(3), settings.ServerSelectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), settings.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.SocketTimeout);
    }

    [Fact]
    public void TheSection_SetsTheWaitsOfTheClient()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Mongo"] = "mongodb://mongo:27017/orders",
            ["Mongo:ServerSelectionTimeout"] = "00:00:02",
            ["Mongo:SocketTimeout"] = "00:00:30",
        }).Build();
        using ServiceProvider provider = new ServiceCollection().AddBaseMongo(configuration).BuildServiceProvider();

        MongoClientSettings settings = provider.GetRequiredService<IMongoClient>().Settings;

        Assert.Equal(TimeSpan.FromSeconds(2), settings.ServerSelectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(10), settings.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.SocketTimeout);
    }

    [Fact]
    public void ABlankConnectionString_IsRejected() => Assert.Throws<ArgumentException>(() => new MongoOptions().Apply(" "));
}
