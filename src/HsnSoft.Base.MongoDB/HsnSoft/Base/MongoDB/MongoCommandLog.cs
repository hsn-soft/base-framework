using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// The log of the commands the driver sends (<see cref="MongoOptions.LogCommands"/>): the name of the command, the database and the collection and how long it took, or the error. The document of the command is <b>never</b> written (it is
/// data), and the commands of the driver itself (the handshake, the heartbeat) are left out.
/// </summary>
internal sealed class MongoCommandLog(ILogger logger)
{
    private static readonly string[] s_driverCommands = ["hello", "isMaster", "ismaster", "ping", "saslStart", "saslContinue", "endSessions", "buildInfo"];

    private static readonly Action<ILogger, string, string, double, Exception?> s_succeeded = LoggerMessage.Define<string, string, double>(LogLevel.Information, new EventId(4101, "MONGO_COMMAND"), "MongoDB {Command} on {Namespace} took {Elapsed} ms");

    private static readonly Action<ILogger, string, string, double, string, Exception?> s_failed = LoggerMessage.Define<string, string, double, string>(LogLevel.Warning, new EventId(4102, "MONGO_COMMAND_FAILED"), "MongoDB {Command} on {Namespace} failed after {Elapsed} ms: {Error}");

    private readonly ConcurrentDictionary<int, string> _namespaces = new();

    /// <summary>Subscribes the log to the events of a client.</summary>
    public void Subscribe(ClusterBuilder builder)
    {
        builder.Subscribe<CommandStartedEvent>(OnStarted);
        builder.Subscribe<CommandSucceededEvent>(OnSucceeded);
        builder.Subscribe<CommandFailedEvent>(OnFailed);
    }

    internal void OnStarted(CommandStartedEvent started) => Started(started.RequestId, started.CommandName, started.DatabaseNamespace.DatabaseName, CollectionOf(started));

    internal void OnSucceeded(CommandSucceededEvent succeeded) => Succeeded(succeeded.RequestId, succeeded.CommandName, succeeded.Duration);

    internal void OnFailed(CommandFailedEvent failed) => Failed(failed.RequestId, failed.CommandName, failed.Duration, failed.Failure);

    public void Started(int requestId, string command, string database, string? collection)
    {
        if (!IsDriverCommand(command))
        {
            _namespaces[requestId] = collection is null ? database : $"{database}.{collection}";
        }
    }

    public void Succeeded(int requestId, string command, TimeSpan duration)
    {
        if (_namespaces.TryRemove(requestId, out string? name))
        {
            s_succeeded(logger, command, name, duration.TotalMilliseconds, null);
        }
    }

    public void Failed(int requestId, string command, TimeSpan duration, Exception failure)
    {
        if (_namespaces.TryRemove(requestId, out string? name))
        {
            s_failed(logger, command, name, duration.TotalMilliseconds, failure.GetType().Name + ": " + failure.Message, null);
        }
    }

    internal static bool IsDriverCommand(string command) => Array.Exists(s_driverCommands, name => string.Equals(name, command, StringComparison.Ordinal));

    // the collection is the value of the first element of the command ({ find: "orders", ... }), when it is a string
    private static string? CollectionOf(CommandStartedEvent started)
        => started.Command.ElementCount > 0 && started.Command.GetElement(0).Value.IsString ? started.Command.GetElement(0).Value.AsString : null;
}
