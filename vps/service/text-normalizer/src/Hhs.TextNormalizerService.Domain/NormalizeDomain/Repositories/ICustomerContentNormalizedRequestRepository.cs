using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;

public interface ICustomerContentNormalizedRequestRepository : IGenericRepository<CustomerContentNormalizedRequest, Guid>
{
    Task<CustomerContentNormalizedRequest?> GetByScopeKeyAndContentIdAsync(string scopeKey, Guid customerContentId, CancellationToken cancellationToken = default);
    Task<List<CustomerContentNormalizedRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
    Task<List<CustomerContentNormalizedRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
