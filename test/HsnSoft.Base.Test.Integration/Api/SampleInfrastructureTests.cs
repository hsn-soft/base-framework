using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HsnSoft.Base.Caching.StackExchangeRedis;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Sample.Api.Application.Contracts;
using HsnSoft.Base.Sample.Api.Application.Contracts.Events;
using HsnSoft.Base.Sample.Api.EventHandlers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Test.Integration.Api;

/// <summary>Event bus (RabbitMQ) and cache (Redis) behaviour of the real sample host.</summary>
[Trait("category", "integration")]
[Collection(SampleApiCollection.Name)]
public class SampleInfrastructureTests(SampleApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PublishedEvent_IsDeliveredThroughRabbitMq_ToItsHandler()
    {
        var store = factory.Services.GetRequiredService<ReceivedEventStore>();
        string userName = "user-" + Guid.NewGuid().ToString("N");

        var response = await _client.PostAsJsonAsync("/api/Events/forgot-password", new { userName, userMailAddress = "user@example.com" });
        response.EnsureSuccessStatusCode();
        string correlationId = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("payload").GetString();

        var received = await WaitAsync(() => store.Get<ForgotPasswordMailSendEto>().FirstOrDefault(x => x.Message.UserName == userName));

        received.Should().NotBeNull("the handler must receive the event published through the broker");
        received.Message.UserMailAddress.Should().Be("user@example.com");
        received.CorrelationId.Should().Be(correlationId);
        received.MessageId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DistributedCache_StoresAndExpiresValues()
    {
        var cache = factory.Services.GetRequiredService<IDistributedCache>();
        string key = "cache-" + Guid.NewGuid().ToString("N");

        await cache.SetStringAsync(key, "value", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2) });
        (await cache.GetStringAsync(key)).Should().Be("value");

        await Task.Delay(TimeSpan.FromSeconds(3));
        (await cache.GetStringAsync(key)).Should().BeNull();
    }

    [Fact]
    public async Task RedisRepository_RoundTripsObjects()
    {
        var repository = factory.Services.GetRequiredService<IRedisRepository<UserDto>>();
        string key = "user-" + Guid.NewGuid().ToString("N");
        var dto = new UserDto { Id = Guid.CreateVersion7(), FullName = "Cached User", Email = "cached@example.com" };

        (await repository.SetDataAsync(key, dto)).Should().BeTrue();
        var loaded = await repository.GetDataAsync(key);
        loaded.Should().BeEquivalentTo(dto);

        (await repository.RemoveDataAsync(key)).Should().BeTrue();
        (await repository.GetDataAsync(key)).Should().BeNull();
    }

    [Fact]
    public async Task RedisLockService_AllowsOnlyOneOwnerAtATime()
    {
        var locks = factory.Services.GetRequiredService<RedisLockService>();
        string key = "lock-" + Guid.NewGuid().ToString("N");

        (await locks.TryAcquireAsync(key, TimeSpan.FromSeconds(30))).Should().BeTrue();
        (await locks.TryAcquireAsync(key, TimeSpan.FromSeconds(30))).Should().BeFalse();

        await locks.ReleaseAsync(key);
        (await locks.TryAcquireAsync(key, TimeSpan.FromSeconds(30))).Should().BeTrue();
    }

    private static async Task<T> WaitAsync<T>(Func<T> probe, int timeoutSeconds = 20) where T : class
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var value = probe();
            if (value != null) return value;
            await Task.Delay(200);
        }

        return null;
    }
}
