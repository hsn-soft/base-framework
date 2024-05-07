namespace HsnSoft.Base.MongoDBOld;

public class MongoDbSettings
{
    public string DatabaseName { get; set; }
    public string ConnectionString { get; set; }
    public int MaxConnectionPoolSize { get; set; }
    public int QueryExecutionMaxSeconds { get; set; } = 60;
}