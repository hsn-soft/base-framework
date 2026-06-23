
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.AuthDomain.Repositories;

public interface IAuthLoginAuditRepository : IGenericRepository<AuthLoginAudit, Guid>
{
}