using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Data;

public class BaseDbConnectionOptions
{
    public ConnectionStrings ConnectionStrings { get; set; }

    public BaseDatabaseInfoDictionary Databases { get; set; }

    public BaseDbConnectionOptions()
    {
        ConnectionStrings = new ConnectionStrings();
        Databases = new BaseDatabaseInfoDictionary();
    }

    public string GetConnectionStringOrNull(
        string connectionStringName,
        bool fallbackToDatabaseMappings = true,
        bool fallbackToDefault = true)
    {
        var connectionString = ConnectionStrings.GetOrDefault(connectionStringName);
        if (!connectionString.IsNullOrEmpty())
        {
            return connectionString;
        }

        if (fallbackToDatabaseMappings)
        {
            var database = Databases.GetMappedDatabaseOrNull(connectionStringName);
            if (database != null)
            {
                connectionString = ConnectionStrings.GetOrDefault(database.DatabaseName);
                if (!connectionString.IsNullOrEmpty())
                {
                    return connectionString;
                }
            }
        }

        if (fallbackToDefault)
        {
            connectionString = ConnectionStrings.Default;
            if (!connectionString.IsNullOrWhiteSpace())
            {
                return connectionString;
            }
        }

        return null;
    }
}