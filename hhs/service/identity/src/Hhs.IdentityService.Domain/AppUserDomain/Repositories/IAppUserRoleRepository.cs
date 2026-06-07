using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.AppUserDomain.Repositories;

public interface IAppUserRoleRepository : IReadOnlyGenericRepository<AppUserRole, Guid>
{
    Task<AppUserRole> CreateAsync(Guid tenantId, Guid userId, Guid roleId);

    Task<int> CreateManyAsync(Guid tenantId, Guid userId, List<Guid> roleIds);

    Task RemoveManyWithUserId(Guid userId);
}