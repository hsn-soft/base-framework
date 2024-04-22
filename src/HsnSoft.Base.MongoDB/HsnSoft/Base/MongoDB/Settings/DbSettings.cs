namespace HsnSoft.Base.MongoDB.Settings;

public class DbSettings : IDbSettings
{
    public string DatabaseName { get; set; }
    public string ConnectionString { get; set; }
    public int MaxConnectionPoolSize { get; set; }
    public int QueryExecutionMaxSeconds { get; set; } = 60;
}