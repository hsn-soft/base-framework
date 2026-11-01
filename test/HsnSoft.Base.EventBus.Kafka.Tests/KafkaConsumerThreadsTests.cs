using System.Reflection;
using System.Reflection.Emit;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class KafkaConsumerThreadsTests
{
    private sealed class Counting : ILogger<KafkaSubscriber>
    {
        private int _listening;

        public int Listening => Volatile.Read(ref _listening);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == "KAFKA_SUBSCRIBER_LISTENING")
            {
                Interlocked.Increment(ref _listening);
            }
        }
    }

    // as many event types as are asked for (an event is named by its type)
    private static Type[] EventTypes(int count)
    {
        ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ConsumerThreadsEvents"), AssemblyBuilderAccess.Run).DefineDynamicModule("events");
        return [.. Enumerable.Range(0, count).Select(number => module.DefineType($"Event{number}Eto", TypeAttributes.Public | TypeAttributes.Class, typeof(object), [typeof(IIntegrationEventMessage)]).CreateType())];
    }

    [Fact]
    public async Task EveryConsumerStartsAtOnce_ThoughtTheyAreMoreThanTheThreadsOfThePool()
    {
        // a consumer blocks a thread in Consume for as long as the service lives: on the pool, this many would be started one thread every half second
        int count = Environment.ProcessorCount * 4;
        EventBusOptions bus = new() { ExchangeName = "XYZ", ConsumerClientName = "Svc" };
        InMemoryEventBusSubscriptionManager subscriptions = new([.. EventTypes(count).Select(type => new EventSubscription(type, typeof(object), 1))], Options.Create(bus), Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryEventBusSubscriptionManager>.Instance);
        Counting log = new();
        KafkaSubscriber subscriber = new(
            subscriptions,
            new EventDispatcher(new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), subscriptions, Options.Create(bus), Microsoft.Extensions.Logging.Abstractions.NullLogger<EventDispatcher>.Instance),
            Options.Create(new KafkaOptions { BootstrapServers = "127.0.0.1:1", CreateTopics = false }),
            Options.Create(bus),
            log);
        using CancellationTokenSource stop = new();

        await subscriber.StartAsync(stop.Token);
        for (int wait = 0; wait < 200 && log.Listening < count; wait++)
        {
            await Task.Delay(25);
        }

        int listening = log.Listening;
        await stop.CancelAsync();
        await subscriber.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(count, listening);
    }
}
