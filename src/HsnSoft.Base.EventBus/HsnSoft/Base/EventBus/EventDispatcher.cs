using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

/// <summary>The broker-independent half of consuming: deserialize, run the handler in its own scope, retry on failure.</summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Handles one message. Returns when the handler succeeded; throws when the message is unusable or the handler still
    /// fails after all attempts, which is the broker project's cue to dead-letter it.
    /// </summary>
    Task DispatchAsync(EventSubscription subscription, ReadOnlyMemory<byte> body, CancellationToken cancellationToken);
}

public sealed partial class EventDispatcher(IServiceScopeFactory scopeFactory, IOptions<EventBusOptions> options, ILogger<EventDispatcher> logger) : IEventDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly EventBusOptions _options = options.Value;
    private readonly ILogger<EventDispatcher> _logger = logger;

    public async Task DispatchAsync(EventSubscription subscription, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        IntegrationEvent integrationEvent = EventSerializer.Deserialize(body.Span, subscription.EventType);
        int maxAttempts = Math.Max(1, _options.MaxHandlerAttempts);

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                await using (scope.ConfigureAwait(false))
                {
                    await subscription.Handle(scope.ServiceProvider, integrationEvent, cancellationToken).ConfigureAwait(false);
                }

                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && exception is not OperationCanceledException)
            {
                LogAttemptFailed(_logger, exception, subscription.EventType.Name, integrationEvent.EventId, attempt, maxAttempts);
                await Task.Delay(_options.RetryDelay * attempt, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Event} {EventId}: attempt {Attempt}/{Max} failed")]
    private static partial void LogAttemptFailed(ILogger logger, Exception exception, string @event, Guid eventId, int attempt, int max);
}
