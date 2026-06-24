using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface ICustomerContentRepository : IGenericRepository<CustomerContent, Guid>
{
    Task<CustomerContent?> GetByScopeKeyAndDomainAsync(string scopeKey, string domainName, CancellationToken cancellationToken = default);
    Task<CustomerContent?> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CustomerContent>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
}
