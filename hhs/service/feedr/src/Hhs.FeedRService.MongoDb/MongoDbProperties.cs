using JetBrains.Annotations;

namespace Hhs.FeedRService.MongoDb;

public static class MongoDbProperties
{
    public static string DbTablePrefix { get; set; } = "";

    [CanBeNull]
    public static string DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Mongo-FeedRService";
}