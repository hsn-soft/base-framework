using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.MultiTenancy.EventBus.Tests;

public sealed record SampleEto(string Name) : IIntegrationEventMessage;

public sealed class TenantRecorder
{
    public Guid? Tenant { get; set; }

    public bool IsSystem { get; set; }
}

public sealed class TenantRecordingHandler(ICurrentTenant tenant, TenantRecorder recorder) : IIntegrationEventHandler<SampleEto>
{
    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken)
    {
        (recorder.Tenant, recorder.IsSystem) = (tenant.Id, tenant.IsSystemTenant);
        return Task.CompletedTask;
    }
}

public class TenantEventBusTests
{
    private sealed class FakeSource(TenantContext? tenant) : ITenantSource
    {
        public TenantContext? Current => tenant;
    }

    private sealed class ScopeLogger : ILogger<TenantEventHandlerScope>
    {
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            Scopes.Add((IReadOnlyDictionary<string, object?>)state);
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }

    private static EventEnvelopeFactory NewFactory(IEventHeaderProvider provider)
        => new(Options.Create(new EventBusOptions { ExchangeName = "ABC", ConsumerClientName = "OrderService" }), [provider]);

    private static ServiceProvider Build(TenantContext? tenant)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ITenantSource>(new FakeSource(tenant));
        services.AddBaseTenantEventBus();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void ThePublishedMessage_CarriesTheTenantInItsHeaders()
    {
        Guid tenant = Guid.NewGuid();
        using ServiceProvider provider = Build(new TenantContext(tenant, IsSystemTenant: true));

        MessageEnvelope<SampleEto> envelope = NewFactory(provider.GetServices<IEventHeaderProvider>().Single()).Create(new SampleEto("m"), null, null, isReQueuePublish: false);

        Assert.Equal(tenant.ToString(), envelope.GetHeader(TenantEventHeaders.TenantId));
        Assert.Equal("true", envelope.GetHeader(TenantEventHeaders.IsSystemTenant));
    }

    [Fact]
    public void AnOrdinaryTenant_HasNoSystemHeader_AndNoTenantMeansNoHeaders()
    {
        using ServiceProvider ordinary = Build(new TenantContext(Guid.NewGuid()));
        using ServiceProvider none = Build(null);

        MessageEnvelope<SampleEto> withTenant = NewFactory(ordinary.GetServices<IEventHeaderProvider>().Single()).Create(new SampleEto("m"), null, null, isReQueuePublish: false);
        MessageEnvelope<SampleEto> without = NewFactory(none.GetServices<IEventHeaderProvider>().Single()).Create(new SampleEto("m"), null, null, isReQueuePublish: false);

        Assert.NotNull(withTenant.GetHeader(TenantEventHeaders.TenantId));
        Assert.Null(withTenant.GetHeader(TenantEventHeaders.IsSystemTenant));
        Assert.Null(without.GetHeader(TenantEventHeaders.TenantId));
        Assert.Null(none.GetServices<IEventHeaderProvider>().Single().GetCorrelationId());
    }

    [Fact]
    public void AChain_KeepsTheTenantItStartedIn()
    {
        Guid started = Guid.NewGuid();
        using ServiceProvider provider = Build(new TenantContext(Guid.NewGuid()));
        ParentMessageEnvelope parent = new();
        parent.SetHeader(TenantEventHeaders.TenantId, started.ToString());

        MessageEnvelope<SampleEto> envelope = NewFactory(provider.GetServices<IEventHeaderProvider>().Single()).Create(new SampleEto("m"), parent, null, isReQueuePublish: false);

        Assert.Equal(started.ToString(), envelope.GetHeader(TenantEventHeaders.TenantId));
    }

    [Fact]
    public void TheScope_MakesTheTenantOfTheMessageTheCurrentTenant_UntilItIsDisposed()
    {
        Guid tenant = Guid.NewGuid();
        Guid own = Guid.NewGuid();
        CurrentTenant current = new([new FakeSource(new TenantContext(own))]);
        ScopeLogger logger = new();
        TenantEventHandlerScope scope = new(current, logger);
        ParentMessageEnvelope envelope = new();
        envelope.SetHeader(TenantEventHeaders.TenantId, tenant.ToString());
        envelope.SetHeader(TenantEventHeaders.IsSystemTenant, "TRUE");

        using (scope.Begin(envelope))
        {
            Assert.Equal(tenant, current.Id);
            Assert.True(current.IsSystemTenant);
        }

        Assert.Equal(own, current.Id);
        Assert.False(current.IsSystemTenant);
        Assert.Equal(tenant, Assert.Single(logger.Scopes)["TenantId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void AMessageWithoutAUsableTenant_ChangesNothing(string? header)
    {
        Guid own = Guid.NewGuid();
        CurrentTenant current = new([new FakeSource(new TenantContext(own))]);
        ScopeLogger logger = new();
        ParentMessageEnvelope envelope = new();
        envelope.SetHeader(TenantEventHeaders.TenantId, header);

        using (new TenantEventHandlerScope(current, logger).Begin(envelope))
        {
            Assert.Equal(own, current.Id);
        }

        Assert.Equal(own, current.Id);
        Assert.Empty(logger.Scopes);
        Assert.Throws<ArgumentNullException>(() => new TenantEventHandlerScope(current, logger).Begin(null!));
    }

    [Fact]
    public async Task TheHandlerOfAMessage_RunsInTheTenantOfTheMessage_AndTheTenantIsGoneAfterwards()
    {
        Guid tenant = Guid.NewGuid();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(Options.Create(new EventBusOptions { MaxHandlerAttempts = 1 }));
        services.AddSingleton<IEventBusSubscriptionManager, InMemoryEventBusSubscriptionManager>();
        services.AddSingleton<TenantRecorder>();
        services.AddBaseTenantEventBus();
        services.AddEventHandler<SampleEto, TenantRecordingHandler>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        EventDispatcher dispatcher = new(provider.GetRequiredService<IServiceScopeFactory>(), provider.GetRequiredService<IEventBusSubscriptionManager>(), Options.Create(new EventBusOptions { MaxHandlerAttempts = 1 }), provider.GetRequiredService<ILogger<EventDispatcher>>());
        MessageEnvelope<SampleEto> envelope = new() { MessageId = Guid.CreateVersion7(), HopLevel = 1, Message = new SampleEto("m") };
        envelope.SetHeader(TenantEventHeaders.TenantId, tenant.ToString());
        envelope.SetHeader(TenantEventHeaders.IsSystemTenant, "true");

        await dispatcher.DispatchAsync("Sample", typeof(SampleEto), envelope);

        TenantRecorder recorder = provider.GetRequiredService<TenantRecorder>();
        Assert.Equal((tenant, true), (recorder.Tenant, recorder.IsSystem));
        Assert.Null(provider.GetRequiredService<ICurrentTenant>().Id);
    }

    [Fact]
    public void TheRegistration_AddsEachServiceOnce_AndRejectsNull()
    {
        ServiceCollection services = new();
        services.AddBaseTenantEventBus().AddBaseTenantEventBus();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IEventHeaderProvider));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IEventHandlerScope));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseTenantEventBus());
    }
}
