using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HsnSoft.Base.Domain.Repositories;

public interface IRedisRepository<T> where T : class, new()
{
    Task<T> GetDataAsync(string dataKey);
    Task<bool> SetDataAsync(string dataKey, T dataValue, TimeSpan? expiry = null);
    Task<bool> RemoveDataAsync(string dataKey);

    Task<IEnumerable<T>> GetDataListAsync(string listKey);
    Task<bool> SetDataListAsync(string listKey, T dataValue);
    Task<bool> RemoveDataFromListAsync(string listKey, T dataValue);
    Task<T> ListLeftPopAsync(string listKey);

    Task<int> SetAndGetIncrementalId(string dataKey);
    Task<T> GetIncrementalDataAsync(string dataKey,int index);
    Task<bool> SetIncrementalDataAsync(string dataKey,int index, T dataValue, TimeSpan? expiry = null);
    Task<bool> RemoveIncrementalDataAsync(string dataKey,int index);
}