using System;
using MongoDB.Driver;

namespace HsnSoft.Base.MongoDB;

/// <summary>
/// How long a service waits for MongoDB (section <c>Mongo</c>). The driver waits 30 seconds for a server and for ever on a connection that has stopped answering (a half-open connection after a network failure), so a request of a
/// pod would hang until the system gives up on the socket; these bound the waits. An option that the connection string names (<c>serverSelectionTimeoutMS</c>, <c>connectTimeoutMS</c>, <c>socketTimeoutMS</c>) is the connection string's.
/// </summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    /// <summary>How long a request waits for a server to be chosen (the cluster is not reachable, or has no primary): then it fails. Default 10 seconds.</summary>
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How long opening a connection to a server may take. Default 10 seconds.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How long a read or a write on a connection may wait (the longest a query or a command can be silent); a connection that does not answer in this time is broken. Default 2 minutes.</summary>
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>The settings of the client for <paramref name="connectionString"/>, with the waits of these options for the ones the connection string does not say.</summary>
    public MongoClientSettings Apply(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
        string text = connectionString;
        if (!Names(text, "serverSelectionTimeout"))
        {
            settings.ServerSelectionTimeout = ServerSelectionTimeout;
        }

        if (!Names(text, "connectTimeout"))
        {
            settings.ConnectTimeout = ConnectTimeout;
        }

        if (!Names(text, "socketTimeout"))
        {
            settings.SocketTimeout = SocketTimeout;
        }

        return settings;
    }

    private static bool Names(string connectionString, string option) => connectionString.Contains(option, StringComparison.OrdinalIgnoreCase);
}
