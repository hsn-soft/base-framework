using System.Linq.Expressions;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using JetBrains.Annotations;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Repositories;

public interface IAppRoleRepository
{
    Task<List<AppRole>> GetPagedListWithFiltersAsync(
        Guid? tenantId,
        [CanBeNull] string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null,
        [CanBeNull] string sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default
    );

    Task<long> GetCountWithFiltersAsync(
        Guid? tenantId,
        [CanBeNull] string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null,
        CancellationToken cancellationToken = default
    );

    Task<List<AppRole>> GetFilterListAsync(
        Guid? tenantId,
        [CanBeNull] string name = null,
        bool? isDefault = null,
        bool? isStatic = null,
        bool? isPublic = null,
        [CanBeNull] string sorting = null,
        CancellationToken cancellationToken = default
    );

    Task<List<AppRole>> GetSearchListAsync(
        Guid? tenantId,
        [CanBeNull] string searchText = null,
        [CanBeNull] string sorting = null,
        int maxResultCount = int.MaxValue,
        CancellationToken cancellationToken = default
    );

    //add default default functions because dbContext is not BaseDbContext
    [ItemCanBeNull]
    Task<AppRole> FindWithIdAsync(Guid id);

    [ItemCanBeNull]
    Task<AppRole> FindAsync(Expression<Func<AppRole, bool>> predicate);

    Task<AppRole> CreateAsync(Guid tenantId, [NotNull] string tenantDomain,
        [NotNull] string name,
        bool isDefault,
        bool isStatic,
        bool isPublic);

    Task<AppRole> CreateAsync(Guid id, Guid tenantId, [NotNull] string tenantDomain,
        [NotNull] string name,
        bool isDefault,
        bool isStatic,
        bool isPublic);


    Task<AppRole> UpdateAsync(Guid id,
        [NotNull] string name,
        bool isDefault,
        bool isStatic,
        bool isPublic);

    Task DeleteAsync(Guid id);
}