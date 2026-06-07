using Hhs.ContentService.Domain.ClientDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Repositories;

public interface IClientRepository : IReadOnlyGenericRepository<Client, Guid>
{
    Task<Client> CreateAsync(
        Guid tenantId,
        [NotNull] string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null);

    Task<Client> CreateAsync(
        Guid id,
        Guid tenantId,
        [NotNull] string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null);

    Task<Client> UpdateAsync(Guid id, [NotNull] string domainName,
        bool isBlocked = false,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null);

    Task RemoveAsync(Guid id);
}