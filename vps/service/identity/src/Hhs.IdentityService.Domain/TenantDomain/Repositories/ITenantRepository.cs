using Hhs.IdentityService.Domain.TenantDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.IdentityService.Domain.TenantDomain.Repositories;

public interface ITenantRepository : IReadOnlyGenericRepository<Tenant, Guid>
{
}