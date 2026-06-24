using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.MongoDb.Repositories;

public sealed class MongoCustomerContentNormalizedRequestRepository(
    IServiceProvider provider,
    TextNormalizerServiceDbContext dbContext
) : MongoGenericRepository<CustomerContentNormalizedRequest, Guid>(provider, dbContext), ICustomerContentNormalizedRequestRepository
{
    [ItemCanBeNull]
    public async Task<CustomerContentNormalizedRequest> GetByScopeKeyAndContentIdAsync(string scopeKey, Guid customerContentId, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.ScopeKey == scopeKey && x.CustomerContentId == customerContentId,
            cancellationToken: cancellationToken
        );

    public async Task<List<CustomerContentNormalizedRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<CustomerContentNormalizedRequest> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }

    public async Task<List<CustomerContentNormalizedRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<CustomerContentNormalizedRequest> { Filter = x => x.Status == status };
        return await GetListAsync(options, cancellationToken);
    }
}
