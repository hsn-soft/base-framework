using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace HsnSoft.Base.MongoDB.Tests;

public class MongoCommandLogTests
{
    private sealed class Memory : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
    }

    [Fact]
    public void ACommandThatSucceeded_IsOneLineWithTheNamespaceAndTheTime_AndNothingOfItsDocument()
    {
        Memory memory = new();
        MongoCommandLog log = new(memory);

        log.Started(7, "find", "orders-db", "orders");
        log.Succeeded(7, "find", TimeSpan.FromMilliseconds(12));

        (LogLevel level, string message) = Assert.Single(memory.Entries);
        Assert.Equal(LogLevel.Information, level);
        Assert.Contains("find", message, StringComparison.Ordinal);
        Assert.Contains("orders-db.orders", message, StringComparison.Ordinal);
        Assert.Contains("12", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACommandThatFailed_IsAWarningWithTheError()
    {
        Memory memory = new();
        MongoCommandLog log = new(memory);

        log.Started(8, "insert", "orders-db", "orders");
        log.Failed(8, "insert", TimeSpan.FromMilliseconds(5), new InvalidOperationException("duplicate key"));

        (LogLevel level, string message) = Assert.Single(memory.Entries);
        Assert.Equal(LogLevel.Warning, level);
        Assert.Contains("duplicate key", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACommandWithoutACollection_IsLoggedWithTheDatabase_AndTheCommandsOfTheDriverAreLeftOut()
    {
        Memory memory = new();
        MongoCommandLog log = new(memory);

        log.Started(1, "listCollections", "orders-db", null);
        log.Succeeded(1, "listCollections", TimeSpan.FromMilliseconds(1));
        log.Started(2, "hello", "admin", null);
        log.Succeeded(2, "hello", TimeSpan.FromMilliseconds(1));
        log.Succeeded(99, "find", TimeSpan.FromMilliseconds(1));   // never started: nothing to say

        string message = Assert.Single(memory.Entries).Message;
        Assert.Contains("orders-db", message, StringComparison.Ordinal);
        Assert.True(MongoCommandLog.IsDriverCommand("ping"));
        Assert.False(MongoCommandLog.IsDriverCommand("find"));
    }

    [Fact]
    public void TheLog_IsOnlyInTheSettingsOfTheClientWhenTheOptionIsOn()
    {
        Memory memory = new();

        Assert.Null(new MongoOptions().Apply("mongodb://mongo:27017/orders", memory).ClusterConfigurator);
        Assert.Null(new MongoOptions { LogCommands = true }.Apply("mongodb://mongo:27017/orders").ClusterConfigurator);
        MongoClientSettings on = new MongoOptions { LogCommands = true }.Apply("mongodb://mongo:27017/orders", memory);

        Assert.NotNull(on.ClusterConfigurator);
        Assert.False(new MongoOptions().LogCommands);
    }

    private static ConnectionId Connection() => new(new ServerId(new global::MongoDB.Driver.Core.Clusters.ClusterId(1), new System.Net.DnsEndPoint("mongo", 27017)), 1);

    [Fact]
    public void TheEventsOfTheDriver_AreWhatTheLogReads_TheCollectionIsTheFirstValueOfTheCommand()
    {
        Memory memory = new();
        MongoCommandLog log = new(memory);
        DatabaseNamespace database = new("orders-db");

        log.OnStarted(new CommandStartedEvent("find", new BsonDocument { { "find", "orders" }, { "filter", new BsonDocument("secret", "value") } }, database, null, 11, Connection()));
        log.OnSucceeded(new CommandSucceededEvent("find", new BsonDocument("ok", 1), database, null, 11, Connection(), TimeSpan.FromMilliseconds(3)));
        log.OnStarted(new CommandStartedEvent("insert", new BsonDocument { { "insert", "orders" } }, database, null, 12, Connection()));
        log.OnFailed(new CommandFailedEvent("insert", database, new InvalidOperationException("boom"), null, 12, Connection(), TimeSpan.FromMilliseconds(4)));
        log.OnStarted(new CommandStartedEvent("count", new BsonDocument { { "count", 5 } }, database, null, 13, Connection()));
        log.OnSucceeded(new CommandSucceededEvent("count", new BsonDocument("ok", 1), database, null, 13, Connection(), TimeSpan.FromMilliseconds(1)));

        Assert.Equal(3, memory.Entries.Count);
        Assert.Contains("orders-db.orders", memory.Entries[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", memory.Entries[0].Message, StringComparison.Ordinal);
        Assert.Contains("boom", memory.Entries[1].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("orders-db.", memory.Entries[2].Message, StringComparison.Ordinal);   // the first value is not a string: the database only
    }

    [Fact]
    public void Subscribing_TakesTheThreeEvents()
    {
        MongoCommandLog log = new(new Memory());

        log.Subscribe(new global::MongoDB.Driver.Core.Configuration.ClusterBuilder());
    }
}
