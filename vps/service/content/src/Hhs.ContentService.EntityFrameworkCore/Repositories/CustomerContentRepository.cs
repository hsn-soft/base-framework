using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreCustomerContentRepository(
    IServiceProvider provider,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<CustomerContent, Guid>(provider, dbContext), ICustomerContentRepository
{
    public async Task<CustomerContent?> GetByScopeKeyAndDomainAsync(string scopeKey, string domainName, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.ScopeKey == scopeKey && x.DomainName == domainName,
            cancellationToken: cancellationToken
        );

    public async Task<CustomerContent?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        => await GetFirstOrDefaultAsync(
            x => x.Id == id,
            q => q.AsTracking(),
            cancellationToken: cancellationToken
        );

    public async Task<List<CustomerContent>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default)
    {
        var options = new ListQueryOptions<CustomerContent> { Filter = x => x.ScopeKey == scopeKey };
        return await GetListAsync(options, cancellationToken);
    }
}
