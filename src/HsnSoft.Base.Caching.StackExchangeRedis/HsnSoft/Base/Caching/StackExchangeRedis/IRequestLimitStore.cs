using System;
using System.Threading.Tasks;

namespace HsnSoft.Base.Caching.StackExchangeRedis;

public interface IRequestLimitStore
{
    Task<long> IncrementAsync(string endpointKey, TimeSpan ttl);
    Task<long> DecrementAsync(string endpointKey);
    Task<long> GetCountAsync(string endpointKey);
    Task SetEndpointLimitAsync(string endpointKey, int limit);
    Task<int?> GetEndpointLimitAsync(string endpointKey);
}