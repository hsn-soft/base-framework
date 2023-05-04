using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace HsnSoft.Base.Data;

public class BaseDatabaseInfoDictionary : Dictionary<string, BaseDatabaseInfo>
{
    public BaseDatabaseInfoDictionary()
    {
        ConnectionIndex = new Dictionary<string, BaseDatabaseInfo>();
    }

    private Dictionary<string, BaseDatabaseInfo> ConnectionIndex { get; set; }

    [CanBeNull]
    public BaseDatabaseInfo GetMappedDatabaseOrNull(string connectionStringName)
    {
        return ConnectionIndex.GetOrDefault(connectionStringName);
    }

    public BaseDatabaseInfoDictionary Configure(string databaseName, Action<BaseDatabaseInfo> configureAction)
    {
        var databaseInfo = this.GetOrAdd(
            databaseName,
            () => new BaseDatabaseInfo(databaseName)
        );

        configureAction(databaseInfo);

        return this;
    }

    /// <summary>
    /// This method should be called if this dictionary changes.
    /// It refreshes indexes for quick access to the connection informations.
    /// </summary>
    public void RefreshIndexes()
    {
        ConnectionIndex = new Dictionary<string, BaseDatabaseInfo>();

        foreach (var databaseInfo in Values)
        {
            foreach (var mappedConnection in databaseInfo.MappedConnections)
            {
                if (ConnectionIndex.ContainsKey(mappedConnection))
                {
                    throw new BaseException(
                        $"A connection name can not map to multiple databases: {mappedConnection}."
                    );
                }

                ConnectionIndex[mappedConnection] = databaseInfo;
            }
        }
    }
}