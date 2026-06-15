namespace Hhs.TextNormalizerService.Mongo;

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = default!;
    public string DatabaseName { get; set; } = default!;
}