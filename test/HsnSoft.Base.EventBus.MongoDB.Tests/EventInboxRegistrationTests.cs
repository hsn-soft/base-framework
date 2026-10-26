using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace HsnSoft.Base.EventBus.MongoDB.Tests;

public sealed class EventInboxRegistrationTests
{
    [Fact]
    public void TheRegistration_GivesTheServiceItsStoreOnTheCollectionItNames()
    {
        // the database is only a handle here: nothing is read or written when the store is made
        IMongoDatabase database = new MongoClient("mongodb://localhost:1").GetDatabase("unit");
        ServiceCollection services = new();
        services.AddSingleton(database);
        services.AddEventInbox("OrderInbox", configure: options => options.MaxRetryCount = 7);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<MongoInboxStore>(scope.ServiceProvider.GetRequiredService<IInboxStore>());
        Assert.NotNull(provider.GetService<InboxEnabled>());
        Assert.Equal(7, provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventInboxOptions>>().Value.MaxRetryCount);
    }

    [Fact]
    public void ADatabaseUnderAKey_IsUsedWhenTheKeyIsGiven()
    {
        IMongoDatabase keyed = new MongoClient("mongodb://localhost:1").GetDatabase("keyed");
        ServiceCollection services = new();
        services.AddKeyedSingleton("stock", keyed);
        services.AddEventInbox(serviceKey: "stock");
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<MongoInboxStore>(scope.ServiceProvider.GetRequiredService<IInboxStore>());
    }

    [Fact]
    public void TheMistakes_AreToldWithTheirParameter()
    {
        Assert.Throws<ArgumentNullException>(() => EventInboxServiceCollectionExtensions.AddEventInbox(null!));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddEventInbox(" "));
    }

    [Fact]
    public async Task AnEventNeedsAName()
    {
        MongoInboxStore store = new(new MongoClient("mongodb://localhost:1").GetDatabase("unit").GetCollection<InboxMessage>("InboxMessage"), Microsoft.Extensions.Options.Options.Create(new EventInboxOptions()));

        await Assert.ThrowsAsync<ArgumentException>(() => store.TryStartAsync(Guid.NewGuid(), " ", null));
    }
}
