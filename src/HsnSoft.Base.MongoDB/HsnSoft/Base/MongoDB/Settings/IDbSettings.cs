namespace HsnSoft.Base.MongoDB.Settings;

public interface IDbSettings
{
    string DatabaseName { get; set; }
    string ConnectionString { get; set; }
    int MaxConnectionPoolSize { get; set; }
    int QueryExecutionMaxSeconds { get; set; }
}