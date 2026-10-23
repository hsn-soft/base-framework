using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public sealed class RabbitMqClientLogTests
{
    /// <summary>Reports the way the client library does: Info, Warn and Error(message, exception as a dictionary).</summary>
    [EventSource(Name = "test-rabbitmq-client")]
    private sealed class ClientEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void Info(string message) => WriteEvent(1, message);

        [Event(2, Level = EventLevel.Warning)]
        public void Warn(string message) => WriteEvent(2, message);

        [Event(3, Level = EventLevel.Error)]
        public void Error(string message, string detail) => WriteEvent(3, message, detail);
    }

    [Fact]
    public void WhatTheClientLibraryReports_ReachesTheLogger_AtTheMatchingLevels()
    {
        using ClientEventSource client = new();
        ListLogger logger = new();
        using RabbitMqClientLogBridge bridge = new(logger, source => source == client);

        client.Info("connected");
        client.Warn("slow broker");
        client.Error("lost", "boom");

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug && entry.Message.Contains("RabbitMQ client: connected", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("slow broker", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("lost", StringComparison.Ordinal));
    }

    [Fact]
    public void ASourceThatExistedBeforeTheBridge_IsListenedTo()
    {
        using ClientEventSource client = new();
        ListLogger logger = new();
        using RabbitMqClientLogBridge bridge = new(logger, source => source == client);

        client.Warn("early");

        Assert.Single(logger.Entries, entry => entry.Message.Contains("early", StringComparison.Ordinal));
    }

    [Fact]
    public void ASourceThatIsCreatedAfterTheBridge_IsListenedTo()
    {
        ListLogger logger = new();
        using RabbitMqClientLogBridge bridge = new(logger, source => source.Name == "test-rabbitmq-client");

        using ClientEventSource client = new();
        client.Warn("late");

        Assert.Single(logger.Entries, entry => entry.Message.Contains("late", StringComparison.Ordinal));
    }

    [Fact]
    public void OtherEventSources_AreIgnored()
    {
        using ClientEventSource other = new();
        ListLogger logger = new();
        using RabbitMqClientLogBridge bridge = new(logger, _ => false);

        other.Warn("not ours");

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void TheExceptionTheClientReports_IsDescribedWithItsTypeMessageAndStackTrace()
    {
        Dictionary<string, object?> detail = new() { ["Type"] = "IOException", ["Message"] = "reset", ["StackTrace"] = "at Somewhere" };

        string text = RabbitMqClientLogBridge.Describe(detail);

        Assert.Contains("IOException: reset", text, StringComparison.Ordinal);
        Assert.Contains("at Somewhere", text, StringComparison.Ordinal);
        Assert.Equal(string.Empty, RabbitMqClientLogBridge.Describe("not a dictionary"));
        Assert.Equal(string.Empty, RabbitMqClientLogBridge.Describe(null));
    }

    [Fact]
    public async Task TheService_StartsAndStopsTheBridge()
    {
        ServiceCollection services = new();
        services.AddLogging();
        using ServiceProvider provider = services.BuildServiceProvider();
        RabbitMqClientLogService service = new(provider.GetRequiredService<ILoggerFactory>());

        await ((IHostedService)service).StartAsync(CancellationToken.None);
        await ((IHostedService)service).StopAsync(CancellationToken.None);
        service.Dispose();
    }
}
