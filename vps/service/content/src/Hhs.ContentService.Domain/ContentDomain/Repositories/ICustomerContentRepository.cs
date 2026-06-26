using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface ICustomerContentRepository : IGenericRepository<CustomerContent, Guid>
{
  Task<CustomerContent> CreateAsync([NotNull] string scopeKey, [NotNull] string contentKey, [CanBeNull] string correlationId = null);


    [ItemCanBeNull]
    Task<CustomerContent> GetByScopeKeyAndSlugKeyAsync(string scopeKey, string slugKey, CancellationToken cancellationToken = default);
    [ItemCanBeNull]
    Task<CustomerContent> GetByIdWithTrackingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<CustomerContent>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
}
