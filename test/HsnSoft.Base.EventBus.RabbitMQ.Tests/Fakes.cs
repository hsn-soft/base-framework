using System.Reflection;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.RabbitMQ.Connection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HsnSoft.Base.EventBus.RabbitMQ.Tests;

public sealed record SampleEto(string Name, int Count) : IIntegrationEventMessage;

public sealed record OtherEto(Guid Id) : IIntegrationEventMessage;

public sealed class SampleHandler : IIntegrationEventHandler<SampleEto>
{
    public Task HandleAsync(MessageEnvelope<SampleEto> envelope, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed record ChannelCall(string Name, object?[] Args);

/// <summary>What happened on a fake channel, and how to make it misbehave.</summary>
public sealed class ChannelState
{
    private readonly Lock _gate = new();

    public List<ChannelCall> Calls { get; } = [];

    public bool IsClosed { get; set; }

    /// <summary>Called before every channel call; the task it returns is awaited before the call completes (to hold a call back).</summary>
    public Func<ChannelCall, Task?>? Before { get; set; }

    /// <summary>Called before every channel call; an exception it returns is thrown from the call.</summary>
    public Func<ChannelCall, Exception?>? Failure { get; set; }

    public IReadOnlyList<ChannelCall> Named(string name)
    {
        lock (_gate)
        {
            return [.. Calls.Where(call => call.Name == name)];
        }
    }

    internal void Add(ChannelCall call)
    {
        lock (_gate)
        {
            Calls.Add(call);
        }
    }
}

/// <summary>An <see cref="IChannel"/> that records what it is asked to do and answers with defaults.</summary>
public class ChannelProxy : DispatchProxy
{
    public ChannelState State { get; set; } = new();

    public static IChannel Create(ChannelState state)
    {
        IChannel channel = Create<IChannel, ChannelProxy>();
        ((ChannelProxy)(object)channel).State = state;
        return channel;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        string name = targetMethod!.Name;
        ChannelCall call = new(name, args ?? []);

        switch (name)
        {
            case "get_IsClosed":
                return State.IsClosed;
            case "get_IsOpen":
                return !State.IsClosed;
            case "get_ChannelNumber":
                return 1;
            case "get_CurrentQueue" or "get_CloseReason" or "get_DefaultConsumer":
                return null;
        }

        if (name.StartsWith("add_", StringComparison.Ordinal) || name.StartsWith("remove_", StringComparison.Ordinal))
        {
            // event subscriptions are recorded too, so a test can find the handler
            State.Add(call);
            return null;
        }

        State.Add(call);
        Exception? failure = State.Failure?.Invoke(call);
        if (failure is not null)
        {
            throw failure;
        }

        if (name is "CloseAsync" or "Dispose" or "DisposeAsync" or "AbortAsync")
        {
            State.IsClosed = true;
        }

        Task? before = State.Before?.Invoke(call);
        return ProxyResult.For(targetMethod.ReturnType, before, name == "BasicConsumeAsync" ? "consumer-tag" : null);
    }
}

/// <summary>The answers of a recording proxy for the return types an async interface has.</summary>
internal static class ProxyResult
{
    internal static object? For(Type returnType, Task? before, string? text)
    {
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Wait(before);
        }

        if (returnType == typeof(ValueTask))
        {
            return new ValueTask(Wait(before));
        }

        if (returnType.IsGenericType)
        {
            Type definition = returnType.GetGenericTypeDefinition();
            Type value = returnType.GetGenericArguments()[0];
            object? result = value == typeof(string) ? text ?? string.Empty : value.IsValueType ? Activator.CreateInstance(value) : null;
            Task task = WaitAndReturn(before, result, value);
            if (definition == typeof(Task<>))
            {
                return task;
            }

            if (definition == typeof(ValueTask<>))
            {
                return Activator.CreateInstance(returnType, task);
            }
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private static async Task Wait(Task? before)
    {
        if (before is not null)
        {
            await before.ConfigureAwait(false);
        }
    }

    private static Task WaitAndReturn(Task? before, object? result, Type valueType)
        => (Task)typeof(ProxyResult).GetMethod(nameof(WaitAndReturnCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(valueType)
            .Invoke(null, [before, result])!;

    private static async Task<T> WaitAndReturnCore<T>(Task? before, object? result)
    {
        if (before is not null)
        {
            await before.ConfigureAwait(false);
        }

        return (T)result!;
    }
}

public sealed class ConnectionState
{
    public List<ChannelCall> Calls { get; } = [];

    public bool IsOpen { get; set; } = true;

    public Exception? CloseFailure { get; set; }

    public ChannelState Channel { get; } = new();

    public IReadOnlyList<ChannelCall> Named(string name) => [.. Calls.Where(call => call.Name == name)];
}

/// <summary>An <see cref="IConnection"/> that records what it is asked to do.</summary>
public class ConnectionProxy : DispatchProxy
{
    public ConnectionState State { get; set; } = new();

    public static IConnection Create(ConnectionState state)
    {
        IConnection connection = Create<IConnection, ConnectionProxy>();
        ((ConnectionProxy)(object)connection).State = state;
        return connection;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        string name = targetMethod!.Name;
        State.Calls.Add(new ChannelCall(name, args ?? []));
        switch (name)
        {
            case "get_IsOpen":
                return State.IsOpen;
            case "get_Endpoint":
                return new AmqpTcpEndpoint("fake-host");
            case "CreateChannelAsync":
                return Task.FromResult(ChannelProxy.Create(State.Channel));
            case "CloseAsync":
                if (State.CloseFailure is not null)
                {
                    throw State.CloseFailure;
                }

                State.IsOpen = false;
                return Task.CompletedTask;
        }

        return ProxyResult.For(targetMethod.ReturnType, null, null);
    }
}

/// <summary>An <see cref="IConnectionFactory"/> that hands out the connections it is given, or fails.</summary>
public class ConnectionFactoryProxy : DispatchProxy
{
    public Queue<Exception> Failures { get; } = new();

    public List<IConnection> Created { get; } = [];

    public Queue<IConnection> Connections { get; } = new();

    public static ConnectionFactoryProxy Create(out IConnectionFactory factory)
    {
        factory = Create<IConnectionFactory, ConnectionFactoryProxy>();
        return (ConnectionFactoryProxy)(object)factory;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == "CreateConnectionAsync")
        {
            if (Failures.Count > 0)
            {
                throw Failures.Dequeue();
            }

            IConnection connection = Connections.Dequeue();
            Created.Add(connection);
            return Task.FromResult(connection);
        }

        return ProxyResult.For(targetMethod.ReturnType, null, null);
    }
}

public sealed class FakeConnection : IRabbitMqPersistentConnection
{
    private readonly Queue<ChannelState> _states = new();

    public bool IsConnected { get; set; } = true;

    public int ConnectCalls { get; private set; }

    public bool ConnectSucceeds { get; set; } = true;

    public Exception? ConnectFailure { get; set; }

    public List<ChannelState> Channels { get; } = [];

    public List<ushort?> DispatchConcurrencies { get; } = [];

    public Exception? CreateChannelFailure { get; set; }

    public int ChannelsToFail { get; set; }

    /// <summary>The state of the next channel that is created; each channel gets a new one when none is queued.</summary>
    public void QueueChannelState(ChannelState state) => _states.Enqueue(state);

    public Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        ConnectCalls++;
        if (ConnectFailure is not null)
        {
            throw ConnectFailure;
        }

        IsConnected = ConnectSucceeds;
        return Task.FromResult(IsConnected);
    }

    public Task<IChannel> CreateModelAsync(ushort? consumerDispatchConcurrency = null, CancellationToken cancellationToken = default)
    {
        DispatchConcurrencies.Add(consumerDispatchConcurrency);
        if (ChannelsToFail > 0)
        {
            ChannelsToFail--;
            throw CreateChannelFailure ?? new InvalidOperationException("channel failure");
        }

        ChannelState state = _states.Count > 0 ? _states.Dequeue() : new ChannelState();
        Channels.Add(state);
        return Task.FromResult(ChannelProxy.Create(state));
    }

    public void Dispose()
    {
    }
}

public sealed class FakeDispatcher : IEventDispatcher
{
    public List<(string EventName, Type EventType, object Envelope)> Dispatched { get; } = [];

    public Func<Task>? Behavior { get; set; }

    public int Running { get; private set; }

    public int MaxRunning { get; private set; }

    public CancellationToken LastToken { get; private set; }

    public async Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope, CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        lock (Dispatched)
        {
            Dispatched.Add((trimmedEventName, eventType, messageEnvelope));
            Running++;
            MaxRunning = Math.Max(MaxRunning, Running);
        }

        try
        {
            if (Behavior is not null)
            {
                await Behavior().ConfigureAwait(false);
            }
        }
        finally
        {
            lock (Dispatched)
            {
                Running--;
            }
        }
    }
}

public sealed class FakeNotifier : IFailedEventNotifier
{
    public List<(string Error, string EventName, string Content)> Notified { get; } = [];

    public Exception? Failure { get; set; }

    public Task NotifyAsync(string errorMessage, string failedEventName, string failedMessageContent, CancellationToken cancellationToken = default)
    {
        Notified.Add((errorMessage, failedEventName, failedMessageContent));
        return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
    }

    public Task NotifyDirectAsync(string errorMessage, string? failedMessageTypeName, object? failedMessageObject, ParentMessageEnvelope? parentContext, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class ListLogger : ILogger
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <summary>Every scope that was started, as its properties.</summary>
    public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            lock (Scopes)
            {
                Scopes.Add(values.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
        }

        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Entries)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}

public sealed class ListLogger<T> : ILogger<T>
{
    private readonly ListLogger _inner = new();

    public List<(LogLevel Level, string Message)> Entries => _inner.Entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _inner.Log(logLevel, eventId, state, exception, formatter);
}
