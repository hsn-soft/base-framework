using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.AppRoleDomain.Repositories;

public interface IAppRoleClaimRepository : IGenericRepository<AppRoleClaim, Guid>
{
}