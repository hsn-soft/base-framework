using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

public interface IAppUserRoleRepository : IReadOnlyGenericRepository<AppUserRole, Guid>
{
    Task<AppUserRole> CreateAsync(Guid userId, Guid roleId);

    Task<int> CreateManyAsync(Guid userId, List<Guid> roleIds);

    Task RemoveManyWithUserId(Guid userId);
}