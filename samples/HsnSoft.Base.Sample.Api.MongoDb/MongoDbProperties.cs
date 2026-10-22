using JetBrains.Annotations;

namespace HsnSoft.Base.Sample.Api.MongoDb;

public static class MongoDbProperties
{
    public static string DbTablePrefix { get; set; } = "";

    [CanBeNull]
    public static string DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Mongo-PerformanceService";
}