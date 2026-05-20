using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreTenantRepository(IServiceProvider provider, IStringLocalizerFactory stringLocalizerFactory, IdentityServiceDbContext dbContext)
    : EfCoreGenericRepository<Tenant, Guid>(provider, dbContext), ITenantRepository
{
}