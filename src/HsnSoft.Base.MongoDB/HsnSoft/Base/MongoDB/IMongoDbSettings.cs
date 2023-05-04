namespace HsnSoft.Base.MongoDB;

public interface IMongoDbSettings
{
    string DatabaseName { get; set; }
    string ConnectionString { get; set; }
    int MaxConnectionPoolSize { get; set; } 
}