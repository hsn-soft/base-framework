using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.AppUserDomain.Repositories;

public interface IAppUserClaimRepository : IGenericRepository<AppUserClaim, Guid>
{
}