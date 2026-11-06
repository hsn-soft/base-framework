using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Tests;

public sealed class EventDispatcherLimitTests : IDisposable
{
    private readonly ServiceProvider _provider;

    private readonly EventDispatcher _dispatcher;

    private readonly HoldingHandler _handler = new();

    public EventDispatcherLimitTests() : this(2)
    {
    }

    private EventDispatcherLimitTests(int limit)
    {
        EventBusOptions options = new() { MaxConcurrentHandlers = limit };
        ServiceCollection services = new();
        services.AddSingleton(Options.Create(options));
        services.AddLogging();
        services.AddSingleton(_handler);
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.AddEventHandler<SampleEto, HoldingHandler>();
        _provider = services.BuildServiceProvider();
        _dispatcher = new EventDispatcher(_provider.GetRequiredService<IServiceScopeFactory>(), _provider.GetRequiredService<IEventBusSubscriptionManager>(), Options.Create(options), NullLogger<EventDispatcher>.Instance);
    }

    public void Dispose() => _provider.Dispose();

    private sealed class HoldingHandler : IIntegrationEventHandler<SampleEto>
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _running;

        public int Started { get; private set; }

        public int MostAtOnce { get; private set; }

        public void Release() => _release.TrySetResult();

        public async Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
        {
            int now = Interlocked.Increment(ref _running);
            lock (this)
            {
                Started++;
                MostAtOnce = Math.Max(MostAtOnce, now);
            }

            try
            {
                await _release.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _running);
            }
        }
    }

    private Task Dispatch(CancellationToken token = default) => _dispatcher.DispatchAsync("Sample", typeof(SampleEto), new MessageEnvelope<SampleEto> { MessageId = Guid.CreateVersion7(), CorrelationId = "c", HopLevel = 1, Message = new SampleEto("m", 1) }, token);

    private static async Task Until(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task OnlyTheLimitOfMessages_AreHandledAtTheSameMoment_TheOthersWaitAndAreThenHandled()
    {
        Task[] all = [.. Enumerable.Range(0, 5).Select(_ => Dispatch())];
        await Until(() => _handler.Started >= 2);
        await Task.Delay(100);

        Assert.Equal(2, _handler.Started);

        _handler.Release();
        await Task.WhenAll(all).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(5, _handler.Started);
        Assert.Equal(2, _handler.MostAtOnce);
    }

    [Fact]
    public async Task AMessageThatWaits_StopsWaitingWhenTheTokenIsCancelled_AndTakesNoPlace()
    {
        Task first = Dispatch();
        Task second = Dispatch();
        await Until(() => _handler.Started == 2);
        using CancellationTokenSource cancel = new();
        Task waiting = Dispatch(cancel.Token);
        await Task.Delay(50);

        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        _handler.Release();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
        await Dispatch().WaitAsync(TimeSpan.FromSeconds(10));   // both places are free again
        Assert.Equal(3, _handler.Started);
    }

    [Fact]
    public async Task WithNoLimit_EveryMessageIsHandledAtOnce()
    {
        using EventDispatcherLimitTests unlimited = new(0);
        Task[] all = [.. Enumerable.Range(0, 6).Select(_ => unlimited.Dispatch())];
        await Until(() => unlimited._handler.Started == 6);

        Assert.Equal(6, unlimited._handler.MostAtOnce);

        unlimited._handler.Release();
        await Task.WhenAll(all);
    }
}
