namespace HsnSoft.Base.MongoDB;

public class MongoDbSettings : IMongoDbSettings
{
    public string DatabaseName { get; set; }
    public string ConnectionString { get; set; }
    public int MaxConnectionPoolSize { get; set; }
}