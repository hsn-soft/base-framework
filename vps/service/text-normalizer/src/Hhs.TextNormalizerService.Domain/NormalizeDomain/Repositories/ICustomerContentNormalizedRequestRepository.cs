using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;

public interface ICustomerContentNormalizedRequestRepository : IMongoGenericRepository<CustomerContentNormalizedRequest, Guid>
{
    [ItemCanBeNull]
    Task<CustomerContentNormalizedRequest> GetByScopeKeyAndContentIdAsync(string scopeKey, Guid customerContentId, CancellationToken cancellationToken = default);
    Task<List<CustomerContentNormalizedRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
    Task<List<CustomerContentNormalizedRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
