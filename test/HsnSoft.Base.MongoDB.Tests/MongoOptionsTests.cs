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
    public void ThePool_KeepsTheDriversDefaults_UnlessTheOptionsSetIt()
    {
        MongoClientSettings driver = MongoClientSettings.FromUrl(new MongoUrl("mongodb://mongo:27017/orders"));

        MongoClientSettings byDefault = new MongoOptions().Apply("mongodb://mongo:27017/orders");
        MongoClientSettings set = new MongoOptions { MaxConnectionPoolSize = 300, MinConnectionPoolSize = 20, WaitQueueTimeout = TimeSpan.FromSeconds(7) }.Apply("mongodb://mongo:27017/orders");

        Assert.Equal(driver.MaxConnectionPoolSize, byDefault.MaxConnectionPoolSize);
        Assert.Equal(driver.MinConnectionPoolSize, byDefault.MinConnectionPoolSize);
        Assert.Equal(driver.WaitQueueTimeout, byDefault.WaitQueueTimeout);
        Assert.Equal(300, set.MaxConnectionPoolSize);
        Assert.Equal(20, set.MinConnectionPoolSize);
        Assert.Equal(TimeSpan.FromSeconds(7), set.WaitQueueTimeout);
    }

    [Fact]
    public void ThePoolOptionsOfTheConnectionString_AreTheConnectionStrings()
    {
        MongoClientSettings settings = new MongoOptions { MaxConnectionPoolSize = 300, MinConnectionPoolSize = 20, WaitQueueTimeout = TimeSpan.FromSeconds(7) }
            .Apply("mongodb://mongo:27017/orders?maxPoolSize=50&minPoolSize=5&waitQueueTimeoutMS=1000");

        Assert.Equal(50, settings.MaxConnectionPoolSize);
        Assert.Equal(5, settings.MinConnectionPoolSize);
        Assert.Equal(TimeSpan.FromSeconds(1), settings.WaitQueueTimeout);
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
