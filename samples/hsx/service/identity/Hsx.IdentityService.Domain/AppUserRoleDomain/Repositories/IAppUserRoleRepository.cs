using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;

namespace Hsx.IdentityService.Domain.AppUserRoleDomain.Repositories;

/// <summary>The repository of the roles of the users.</summary>
public interface IAppUserRoleRepository : IRepository<AppUserRole, Guid>
{
    /// <summary>The users that have the role (a role is not deleted while it has users, and the sessions of its users end when it loses a subscription).</summary>
    Task<IReadOnlyList<Guid>> GetUserIdsOfRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
