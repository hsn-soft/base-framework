
namespace HsnSoft.Base.EventBus.Tests;

public sealed record SampleEvent(string Name, int Count) : IntegrationEvent;

public sealed record OtherEvent(Guid Id) : IntegrationEvent;

/// <summary>Records what it handled and fails the first <see cref="FailTimes"/> calls.</summary>
public sealed class RecordingHandler : IIntegrationEventHandler<SampleEvent>
{
    public List<SampleEvent> Handled { get; } = [];

    public int Calls { get; private set; }

    public int FailTimes { get; set; }

    public Exception? Failure { get; set; }

    public Task HandleAsync(SampleEvent integrationEvent, CancellationToken cancellationToken)
    {
        Calls++;
        if (Calls <= FailTimes)
        {
            throw Failure ?? new InvalidOperationException($"failure {Calls}");
        }

        Handled.Add(integrationEvent);
        return Task.CompletedTask;
    }
}
