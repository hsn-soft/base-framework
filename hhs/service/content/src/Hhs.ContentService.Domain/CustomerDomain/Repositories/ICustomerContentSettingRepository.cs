using Hhs.ContentService.Domain.CustomerDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.CustomerDomain.Repositories;

public interface ICustomerContentSettingRepository : IReadOnlyGenericRepository<CustomerContentSetting, Guid>
{
    Task<CustomerContentSetting> CreateAsync(
        Guid tenantId,
        [NotNull] string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null);

    Task<CustomerContentSetting> CreateAsync(
        Guid id,
        Guid tenantId,
        [NotNull] string domainName,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null);

    Task<CustomerContentSetting> UpdateAsync(Guid id, [NotNull] string domainName,
        bool isBlocked = false,
        List<string> includePathFilters = null,
        List<string> excludePathFilters = null);

    Task RemoveAsync(Guid id);
}